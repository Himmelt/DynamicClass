using DynamicCompiler.Models;
using Microsoft.CodeAnalysis;
using System.Reflection;
using System.Text.RegularExpressions;

namespace DynamicCompiler.Core {
    /// <summary>
    /// 代码分析器，负责分析代码并检测所需的程序集引用
    /// </summary>
    internal static class CodeAnalyzer {
        /// <summary>
        /// 程序集检测规则
        /// </summary>
        private static readonly Dictionary<string, AssemblyDetectionRule[]> AssemblyDetectionRules = new() {
            // 核心 .NET 程序集
            ["System.Linq"] =
            [
                new AssemblyDetectionRule("System.Linq", r =>
                    r.Contains("Linq") ||
                    Regex.IsMatch(r, @"\b(?:IEnumerable|IQueryable|Select|Where|OrderBy|ThenBy|GroupBy|Join|SelectMany|Any|All|First|Last|Single|ToArray|ToList|ToDictionary|Count|Sum|Average|Min|Max)\b", RegexOptions.IgnoreCase))
            ],

            ["System.Collections.Generic"] =
            [
                new AssemblyDetectionRule("System.Collections.Generic", r =>
                    Regex.IsMatch(r, @"\b(?:List<|Dictionary<|HashSet<|IEnumerable<|IList<|ICollection<|Queue<|Stack<|SortedList<|ObservableCollection<|IQueryable<)\b", RegexOptions.IgnoreCase))
            ],

            // JSON处理
            ["System.Text.Json"] =
            [
                new AssemblyDetectionRule("System.Text.Json", r =>
                    r.Contains("JsonSerializer") || r.Contains("JsonElement") || r.Contains("JsonDocument") ||
                    Regex.IsMatch(r, @"\b(?:JsonSerializer|JsonElement|JsonDocument|JsonPropertyName|JsonException|JsonParseException)\b", RegexOptions.IgnoreCase))
            ],

            ["Newtonsoft.Json"] =
            [
                new AssemblyDetectionRule("Newtonsoft.Json", r =>
                    r.Contains("JsonConvert") || r.Contains("JObject") || r.Contains("Newtonsoft.Json") ||
                    Regex.IsMatch(r, @"\b(?:JObject|JArray|JToken|JsonException|JsonSerializerSettings|Newtonsoft\.Json)\b", RegexOptions.IgnoreCase))
            ],

            // HTTP 和网络
            ["System.Net.Http"] =
            [
                new AssemblyDetectionRule("System.Net.Http", r =>
                    r.Contains("HttpClient") || r.Contains("HttpResponseMessage") ||
                    Regex.IsMatch(r, @"\b(?:HttpClient|HttpResponseMessage|HttpRequestException|HttpContent|MultipartFormDataContent)\b", RegexOptions.IgnoreCase))
            ],

            // 正则表达式
            ["System.Text.RegularExpressions"] =
            [
                new AssemblyDetectionRule("System.Text.RegularExpressions", r =>
                    r.Contains("Regex") ||
                    Regex.IsMatch(r, @"\b(?:Regex|Match|MatchCollection|Group|Capture|RegexOptions|MatchTimeoutException)\b", RegexOptions.IgnoreCase))
            ],

            // 诊断和调试
            ["System.Diagnostics"] =
            [
                new AssemblyDetectionRule("System.Diagnostics", r =>
                    r.Contains("Stopwatch") ||
                    Regex.IsMatch(r, @"\b(?:Stopwatch|Process|EventLog|EventLogEntry|TraceListener|CounterSample)\b", RegexOptions.IgnoreCase))
            ],

            // 数据库访问 - 使用System.Data.Common而不是System.Data
            ["System.Data"] =
            [
                new AssemblyDetectionRule("System.Data.Common", r =>
                    Regex.IsMatch(r, @"\b(?:DataTable|DataColumn|DataRow|DataSet|DataAdapter|DataView|CommonDbDataAdapter)\b", RegexOptions.IgnoreCase))
            ],

            ["System.Data.SqlClient"] =
            [
                new AssemblyDetectionRule("System.Data.SqlClient", r =>
                    Regex.IsMatch(r, @"\b(?:SqlConnection|SqlCommand|SqlDataReader|SqlTransaction|SqlParameter|SqlException)\b", RegexOptions.IgnoreCase))
            ],

            // Entity Framework
            ["Microsoft.EntityFrameworkCore"] =
            [
                new AssemblyDetectionRule("Microsoft.EntityFrameworkCore", r =>
                    r.Contains("DbContext") || r.Contains("DbSet") ||
                    Regex.IsMatch(r, @"\b(?:DbContext|DbSet<|IQueryable<Entity>|OnConfiguring|OnModelCreating|Entity<)\b", RegexOptions.IgnoreCase))
            ],

            // 现代 .NET 扩展库
            ["Microsoft.Extensions.Configuration"] =
            [
                new AssemblyDetectionRule("Microsoft.Extensions.Configuration", r =>
                    Regex.IsMatch(r, @"\b(?:IConfiguration|IConfigurationBuilder|IConfigurationRoot|ConfigurationBuilder)\b", RegexOptions.IgnoreCase))
            ],

            ["Microsoft.Extensions.DependencyInjection"] =
            [
                new AssemblyDetectionRule("Microsoft.Extensions.DependencyInjection", r =>
                    Regex.IsMatch(r, @"\b(?:IServiceProvider|IServiceCollection|ServiceDescriptor|ServiceLifetime)\b", RegexOptions.IgnoreCase))
            ],

            ["Microsoft.Extensions.Logging"] =
            [
                new AssemblyDetectionRule("Microsoft.Extensions.Logging", r =>
                    Regex.IsMatch(r, @"\b(?:ILogger|ILoggerFactory|ILoggerProvider|LogLevel|LogInformation|LogWarning|LogError|LogDebug)\b", RegexOptions.IgnoreCase))
            ],

            // ASP.NET Core
            ["Microsoft.AspNetCore.Http"] =
            [
                new AssemblyDetectionRule("Microsoft.AspNetCore.Http", r =>
                    Regex.IsMatch(r, @"\b(?:HttpContext|HttpRequest|HttpResponse|HttpContextAccessor)\b", RegexOptions.IgnoreCase))
            ],

            // Windows Forms（如果需要）
            ["System.Windows.Forms"] =
            [
                new AssemblyDetectionRule("System.Windows.Forms", r =>
                    Regex.IsMatch(r, @"\b(?:Form|Button|TextBox|Control|Application)\b", RegexOptions.IgnoreCase))
            ]
        };

        /// <summary>
        /// 智能分析代码并返回所需的程序集引用
        /// </summary>
        /// <param name="code">要分析的C#代码</param>
        /// <returns>所需的程序集引用数组</returns>
        internal static MetadataReference[] GetRequiredReferences(string code) {
            var requiredReferences = new List<MetadataReference>();

            // 基础引用（总是需要的）
            var baseReferences = new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System").Location)
            };
            requiredReferences.AddRange(baseReferences);

            // 提取using语句
            var usingStatements = ExtractUsingStatements(code);

            // 使用多层级检测策略
            var detectedAssemblies = DetectAssemblies(code, usingStatements);

            // 根据检测到的程序集添加引用
            foreach (var assemblyName in detectedAssemblies) {
                try {
                    var assembly = Assembly.Load(assemblyName);
                    var metadataRef = MetadataReference.CreateFromFile(assembly.Location);
                    requiredReferences.Add(metadataRef);
                } catch (Exception ex) {
                    // 记录无法加载的程序集（用于调试）
                    System.Diagnostics.Debug.WriteLine($"无法加载程序集 {assemblyName}: {ex.Message}");
                }
            }

            return [.. requiredReferences];
        }

        /// <summary>
        /// 多层级程序集检测策略
        /// </summary>
        /// <param name="code">C#代码</param>
        /// <param name="usingStatements">using语句列表</param>
        /// <returns>检测到的程序集名称列表</returns>
        private static HashSet<string> DetectAssemblies(string code, HashSet<string> usingStatements) {
            var detectedAssemblies = new HashSet<string>();

            // 第一层：基于using语句的检测（最准确）
            foreach (var usingStatement in usingStatements) {
                foreach (var ruleGroup in AssemblyDetectionRules) {
                    foreach (var rule in ruleGroup.Value) {
                        if (usingStatement.StartsWith(ExtractAssemblyNamespace(rule.AssemblyName), StringComparison.OrdinalIgnoreCase)) {
                            detectedAssemblies.Add(rule.AssemblyName);
                        }
                    }
                }
            }

            // 第二层：基于预定义规则的模式匹配
            foreach (var ruleGroup in AssemblyDetectionRules) {
                foreach (var rule in ruleGroup.Value) {
                    if (rule.DetectionFunction(code)) {
                        detectedAssemblies.Add(rule.AssemblyName);
                    }
                }
            }

            // 第三层：基于.NET运行时信息的智能推断
            var inferredAssemblies = InferAssembliesFromUsage(code);
            foreach (var assembly in inferredAssemblies) {
                detectedAssemblies.Add(assembly);
            }

            return detectedAssemblies;
        }

        /// <summary>
        /// 从代码中提取using语句
        /// </summary>
        /// <param name="code">C#代码</param>
        /// <returns>using语句集合</returns>
        internal static HashSet<string> ExtractUsingStatements(string code) {
            var usingStatements = new HashSet<string>();
            var usingRegex = new Regex(@"using\s+([^;]+);", RegexOptions.Multiline);

            foreach (Match match in usingRegex.Matches(code)) {
                if (match.Success && match.Groups.Count > 1) {
                    usingStatements.Add(match.Groups[1].Value.Trim());
                }
            }

            return usingStatements;
        }

        /// <summary>
        /// 根据程序集名称提取基础命名空间
        /// </summary>
        /// <param name="assemblyName">程序集名称</param>
        /// <returns>基础命名空间</returns>
        private static string ExtractAssemblyNamespace(string assemblyName) {
            var mapping = new Dictionary<string, string> {
                ["System.Data"] = "System.Data",
                ["System.IO.FileSystem"] = "System.IO",
                ["System.Net.Http"] = "System.Net.Http",
                ["System.Text.Json"] = "System.Text.Json",
                ["System.Linq"] = "System.Linq",
                ["System.Collections.Generic"] = "System.Collections.Generic",
                ["Microsoft.EntityFrameworkCore"] = "Microsoft.EntityFrameworkCore",
                ["Newtonsoft.Json"] = "Newtonsoft.Json",
                ["System.Windows.Forms"] = "System.Windows.Forms",
                ["Microsoft.Extensions.DependencyInjection"] = "Microsoft.Extensions.DependencyInjection"
            };

            return mapping.TryGetValue(assemblyName, out var namespaceName) ? namespaceName : assemblyName;
        }

        /// <summary>
        /// 基于.NET运行时信息的智能推断
        /// </summary>
        /// <param name="code">C#代码</param>
        /// <returns>推断的程序集名称</returns>
        private static HashSet<string> InferAssembliesFromUsage(string code) {
            var inferredAssemblies = new HashSet<string>();

            // 基于.NET标准库的类型推断
            var typeInference = new[]
            {
                new { Pattern = @"\b(DateTime|DateOnly|TimeOnly|TimeSpan)\b", Assembly = "System.Runtime" },
                new { Pattern = @"\b(Guid)\b", Assembly = "System.Runtime" },
                new { Pattern = @"\b(Stopwatch|Timer)\b", Assembly = "System.Diagnostics" },
                new { Pattern = @"\b(MemoryStream|FileStream|CryptoStream)\b", Assembly = "System.IO" },
                new { Pattern = @"\b(Regex|Match|MatchCollection)\b", Assembly = "System.Text.RegularExpressions" }
            };

            foreach (var inference in typeInference) {
                if (Regex.IsMatch(code, inference.Pattern)) {
                    inferredAssemblies.Add(inference.Assembly);
                }
            }

            return inferredAssemblies;
        }

        /// <summary>
        /// 分析代码中使用的类型
        /// </summary>
        /// <param name="code">要分析的C#代码</param>
        /// <returns>使用的类型集合</returns>
        internal static HashSet<string> AnalyzeUsedTypes(string code) {
            var usedTypes = new HashSet<string>();

            // 提取using语句
            var usingRegex = new Regex(@"using\s+([^;]+);", RegexOptions.Multiline);
            foreach (Match match in usingRegex.Matches(code)) {
                if (match.Success && match.Groups.Count > 1) {
                    string namespaceName = match.Groups[1].Value.Trim();
                    usedTypes.Add(namespaceName);
                }
            }

            // 提取类型使用模式
            var typePatterns = new[]
            {
                // 泛型集合
                new Regex(@"\b(List|Dictionary|HashSet|Queue|Stack|LinkedList|SortedSet|SortedDictionary)<", RegexOptions.IgnoreCase),
                // System.Data类型
                new Regex(@"\b(DataTable|DataSet|DataRow|DataColumn)\b", RegexOptions.IgnoreCase),
                // IO类型
                new Regex(@"\b(File|Directory|Path|Stream|StreamReader|StreamWriter)\b", RegexOptions.IgnoreCase),
                // 网络类型
                new Regex(@"\b(HttpClient|WebClient|TcpClient)\b", RegexOptions.IgnoreCase),
                // JSON类型
                new Regex(@"\b(JsonSerializer|JsonDocument|JsonNode)\b", RegexOptions.IgnoreCase),
                // LINQ类型
                new Regex(@"\b(IEnumerable|IQueryable|GroupJoin|SelectMany)\b", RegexOptions.IgnoreCase)
            };

            foreach (var pattern in typePatterns) {
                if (pattern.IsMatch(code)) {
                    string patternString = pattern.ToString();
                    // 根据匹配的类型模式添加相应的命名空间
                    if (patternString.Contains("List|Dictionary|HashSet")) {
                        usedTypes.Add("System.Collections.Generic");
                    } else if (patternString.Contains("DataTable|DataSet")) {
                        usedTypes.Add("System.Data");
                    } else if (patternString.Contains("File|Directory|Path")) {
                        usedTypes.Add("System.IO");
                    } else if (patternString.Contains("HttpClient|WebClient")) {
                        usedTypes.Add("System.Net.Http");
                    } else if (patternString.Contains("JsonSerializer")) {
                        usedTypes.Add("System.Text.Json");
                    } else if (patternString.Contains("IEnumerable|IQueryable")) {
                        usedTypes.Add("System.Linq");
                    }
                }
            }

            return usedTypes;
        }

        /// <summary>
        /// 动态扩展检测规则的方法
        /// </summary>
        /// <param name="assemblyName">程序集名称</param>
        /// <param name="namespacePattern">命名空间模式</param>
        /// <param name="typePatterns">类型模式</param>
        internal static void RegisterAssemblyRule(string assemblyName, string namespacePattern, params string[] typePatterns) {
            var rules = new List<AssemblyDetectionRule> {
                // 添加命名空间检测规则
                new(assemblyName, code =>
                    Regex.IsMatch(code, $@"using\s+{namespacePattern}"))
            };

            // 添加类型检测规则
            foreach (var pattern in typePatterns) {
                rules.Add(new AssemblyDetectionRule(assemblyName, code =>
                    Regex.IsMatch(code, pattern)));
            }

            if (AssemblyDetectionRules.TryGetValue(assemblyName, out AssemblyDetectionRule[]? value)) {
                AssemblyDetectionRules[assemblyName] = [.. value, .. rules];
            } else {
                AssemblyDetectionRules[assemblyName] = [.. rules];
            }
        }
    }
}
