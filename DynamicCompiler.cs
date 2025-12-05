using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace DynamicClass
{
    /// <summary>
    /// 动态编译工具类，用于编译C#静态类代码并将方法转换为Func委托
    /// </summary>
    public class DynamicCompiler
    {
        /// <summary>
        /// 程序集检测规则
        /// </summary>
        private static readonly Dictionary<string, AssemblyDetectionRule[]> AssemblyDetectionRules = new()
        {
            // 核心 .NET 程序集
            ["System.Linq"] = new[]
            {
                new AssemblyDetectionRule("System.Linq", r => 
                    r.Contains("Linq") || 
                    Regex.IsMatch(r, @"\b(?:IEnumerable|IQueryable|Select|Where|OrderBy|ThenBy|GroupBy|Join|SelectMany|Any|All|First|Last|Single|ToArray|ToList|ToDictionary|Count|Sum|Average|Min|Max)\b", RegexOptions.IgnoreCase))
            },
            
            ["System.Collections.Generic"] = new[]
            {
                new AssemblyDetectionRule("System.Collections.Generic", r => 
                    Regex.IsMatch(r, @"\b(?:List<|Dictionary<|HashSet<|IEnumerable<|IList<|ICollection<|Queue<|Stack<|SortedList<|ObservableCollection<|IQueryable<)\b", RegexOptions.IgnoreCase))
            },
            
            // JSON处理
            ["System.Text.Json"] = new[]
            {
                new AssemblyDetectionRule("System.Text.Json", r => 
                    r.Contains("JsonSerializer") || r.Contains("JsonElement") || r.Contains("JsonDocument") || 
                    Regex.IsMatch(r, @"\b(?:JsonSerializer|JsonElement|JsonDocument|JsonPropertyName|JsonException|JsonParseException)\b", RegexOptions.IgnoreCase))
            },
            
            ["Newtonsoft.Json"] = new[]
            {
                new AssemblyDetectionRule("Newtonsoft.Json", r => 
                    r.Contains("JsonConvert") || r.Contains("JObject") || r.Contains("Newtonsoft.Json") ||
                    Regex.IsMatch(r, @"\b(?:JObject|JArray|JToken|JsonException|JsonSerializerSettings|Newtonsoft\.Json)\b", RegexOptions.IgnoreCase))
            },
            
            // HTTP 和网络
            ["System.Net.Http"] = new[]
            {
                new AssemblyDetectionRule("System.Net.Http", r => 
                    r.Contains("HttpClient") || r.Contains("HttpResponseMessage") ||
                    Regex.IsMatch(r, @"\b(?:HttpClient|HttpResponseMessage|HttpRequestException|HttpContent|MultipartFormDataContent)\b", RegexOptions.IgnoreCase))
            },
            
            // 正则表达式
            ["System.Text.RegularExpressions"] = new[]
            {
                new AssemblyDetectionRule("System.Text.RegularExpressions", r => 
                    r.Contains("Regex") || 
                    Regex.IsMatch(r, @"\b(?:Regex|Match|MatchCollection|Group|Capture|RegexOptions|MatchTimeoutException)\b", RegexOptions.IgnoreCase))
            },
            
            // 诊断和调试
            ["System.Diagnostics"] = new[]
            {
                new AssemblyDetectionRule("System.Diagnostics", r => 
                    r.Contains("Stopwatch") || 
                    Regex.IsMatch(r, @"\b(?:Stopwatch|Process|EventLog|EventLogEntry|TraceListener|CounterSample)\b", RegexOptions.IgnoreCase))
            },
            
            // 数据库访问 - 使用System.Data.Common而不是System.Data
            ["System.Data"] = new[]
            {
                new AssemblyDetectionRule("System.Data.Common", r => 
                    Regex.IsMatch(r, @"\b(?:DataTable|DataColumn|DataRow|DataSet|DataAdapter|DataView|CommonDbDataAdapter)\b", RegexOptions.IgnoreCase))
            },
            
            ["System.Data.SqlClient"] = new[]
            {
                new AssemblyDetectionRule("System.Data.SqlClient", r => 
                    Regex.IsMatch(r, @"\b(?:SqlConnection|SqlCommand|SqlDataReader|SqlTransaction|SqlParameter|SqlException)\b", RegexOptions.IgnoreCase))
            },
            
            // Entity Framework
            ["Microsoft.EntityFrameworkCore"] = new[]
            {
                new AssemblyDetectionRule("Microsoft.EntityFrameworkCore", r => 
                    r.Contains("DbContext") || r.Contains("DbSet") || 
                    Regex.IsMatch(r, @"\b(?:DbContext|DbSet<|IQueryable<Entity>|OnConfiguring|OnModelCreating|Entity<)\b", RegexOptions.IgnoreCase))
            },
            
            // 现代 .NET 扩展库
            ["Microsoft.Extensions.Configuration"] = new[]
            {
                new AssemblyDetectionRule("Microsoft.Extensions.Configuration", r => 
                    Regex.IsMatch(r, @"\b(?:IConfiguration|IConfigurationBuilder|IConfigurationRoot|ConfigurationBuilder)\b", RegexOptions.IgnoreCase))
            },
            
            ["Microsoft.Extensions.DependencyInjection"] = new[]
            {
                new AssemblyDetectionRule("Microsoft.Extensions.DependencyInjection", r => 
                    Regex.IsMatch(r, @"\b(?:IServiceProvider|IServiceCollection|ServiceDescriptor|ServiceLifetime)\b", RegexOptions.IgnoreCase))
            },
            
            ["Microsoft.Extensions.Logging"] = new[]
            {
                new AssemblyDetectionRule("Microsoft.Extensions.Logging", r => 
                    Regex.IsMatch(r, @"\b(?:ILogger|ILoggerFactory|ILoggerProvider|LogLevel|LogInformation|LogWarning|LogError|LogDebug)\b", RegexOptions.IgnoreCase))
            },
            
            // ASP.NET Core
            ["Microsoft.AspNetCore.Http"] = new[]
            {
                new AssemblyDetectionRule("Microsoft.AspNetCore.Http", r => 
                    Regex.IsMatch(r, @"\b(?:HttpContext|HttpRequest|HttpResponse|HttpContextAccessor)\b", RegexOptions.IgnoreCase))
            },
            
            // Windows Forms（如果需要）
            ["System.Windows.Forms"] = new[]
            {
                new AssemblyDetectionRule("System.Windows.Forms", r => 
                    Regex.IsMatch(r, @"\b(?:Form|Button|TextBox|Control|Application)\b", RegexOptions.IgnoreCase))
            }
        };

        /// <summary>
        /// 程序集检测规则类
        /// </summary>
        private class AssemblyDetectionRule
        {
            public string AssemblyName { get; }
            public Func<string, bool> DetectionFunction { get; }

            public AssemblyDetectionRule(string assemblyName, Func<string, bool> detectionFunction)
            {
                AssemblyName = assemblyName;
                DetectionFunction = detectionFunction;
            }
        }

        /// <summary>
        /// 智能分析代码并返回所需的程序集引用
        /// </summary>
        /// <param name="code">要分析的C#代码</param>
        /// <returns>所需的程序集引用数组</returns>
        private MetadataReference[] GetRequiredReferences(string code)
        {
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
            foreach (var assemblyName in detectedAssemblies)
            {
                try
                {
                    var assembly = Assembly.Load(assemblyName);
                    var metadataRef = MetadataReference.CreateFromFile(assembly.Location);
                    requiredReferences.Add(metadataRef);
                }
                catch (Exception ex)
                {
                    // 记录无法加载的程序集（用于调试）
                    System.Diagnostics.Debug.WriteLine($"无法加载程序集 {assemblyName}: {ex.Message}");
                }
            }

            return requiredReferences.ToArray();
        }

        /// <summary>
        /// 多层级程序集检测策略
        /// </summary>
        /// <param name="code">C#代码</param>
        /// <param name="usingStatements">using语句列表</param>
        /// <returns>检测到的程序集名称列表</returns>
        private HashSet<string> DetectAssemblies(string code, HashSet<string> usingStatements)
        {
            var detectedAssemblies = new HashSet<string>();

            // 第一层：基于using语句的检测（最准确）
            foreach (var usingStatement in usingStatements)
            {
                foreach (var ruleGroup in AssemblyDetectionRules)
                {
                    foreach (var rule in ruleGroup.Value)
                    {
                        if (usingStatement.StartsWith(ExtractAssemblyNamespace(rule.AssemblyName), StringComparison.OrdinalIgnoreCase))
                        {
                            detectedAssemblies.Add(rule.AssemblyName);
                        }
                    }
                }
            }

            // 第二层：基于预定义规则的模式匹配
            foreach (var ruleGroup in AssemblyDetectionRules)
            {
                foreach (var rule in ruleGroup.Value)
                {
                    if (rule.DetectionFunction(code))
                    {
                        detectedAssemblies.Add(rule.AssemblyName);
                    }
                }
            }

            // 第三层：基于.NET运行时信息的智能推断
            var inferredAssemblies = InferAssembliesFromUsage(code);
            foreach (var assembly in inferredAssemblies)
            {
                detectedAssemblies.Add(assembly);
            }

            return detectedAssemblies;
        }

        /// <summary>
        /// 从代码中提取using语句
        /// </summary>
        /// <param name="code">C#代码</param>
        /// <returns>using语句集合</returns>
        private HashSet<string> ExtractUsingStatements(string code)
        {
            var usingStatements = new HashSet<string>();
            var usingRegex = new Regex(@"using\s+([^;]+);", RegexOptions.Multiline);

            foreach (Match match in usingRegex.Matches(code))
            {
                if (match.Success && match.Groups.Count > 1)
                {
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
        private string ExtractAssemblyNamespace(string assemblyName)
        {
            var mapping = new Dictionary<string, string>
            {
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
        private HashSet<string> InferAssembliesFromUsage(string code)
        {
            var inferredAssemblies = new HashSet<string>();

            // 智能推断逻辑
            var lowerCode = code.ToLowerInvariant();

            // 基于.NET标准库的类型推断
            var typeInference = new[]
            {
                new { Pattern = @"\b(DateTime|DateOnly|TimeOnly|TimeSpan)\b", Assembly = "System.Runtime" },
                new { Pattern = @"\b(Guid)\b", Assembly = "System.Runtime" },
                new { Pattern = @"\b(Stopwatch|Timer)\b", Assembly = "System.Diagnostics" },
                new { Pattern = @"\b(MemoryStream|FileStream|CryptoStream)\b", Assembly = "System.IO" },
                new { Pattern = @"\b(Regex|Match|MatchCollection)\b", Assembly = "System.Text.RegularExpressions" }
            };

            foreach (var inference in typeInference)
            {
                if (Regex.IsMatch(code, inference.Pattern))
                {
                    inferredAssemblies.Add(inference.Assembly);
                }
            }

            return inferredAssemblies;
        }

        /// <summary>
        /// 动态扩展检测规则的方法
        /// </summary>
        /// <param name="assemblyName">程序集名称</param>
        /// <param name="namespacePattern">命名空间模式</param>
        /// <param name="typePatterns">类型模式</param>
        public void RegisterAssemblyRule(string assemblyName, string namespacePattern, params string[] typePatterns)
        {
            var rules = new List<AssemblyDetectionRule>();

            // 添加命名空间检测规则
            rules.Add(new AssemblyDetectionRule(assemblyName, code => 
                Regex.IsMatch(code, $@"using\s+{namespacePattern}")));

            // 添加类型检测规则
            foreach (var pattern in typePatterns)
            {
                rules.Add(new AssemblyDetectionRule(assemblyName, code => 
                    Regex.IsMatch(code, pattern)));
            }

            if (AssemblyDetectionRules.ContainsKey(assemblyName))
            {
                AssemblyDetectionRules[assemblyName] = AssemblyDetectionRules[assemblyName].Concat(rules).ToArray();
            }
            else
            {
                AssemblyDetectionRules[assemblyName] = rules.ToArray();
            }
        }

        /// <summary>
        /// 分析代码中使用的类型
        /// </summary>
        /// <param name="code">要分析的C#代码</param>
        /// <returns>使用的类型集合</returns>
        private HashSet<string> AnalyzeUsedTypes(string code)
        {
            var usedTypes = new HashSet<string>();

            // 提取using语句
            var usingRegex = new Regex(@"using\s+([^;]+);", RegexOptions.Multiline);
            foreach (Match match in usingRegex.Matches(code))
            {
                if (match.Success && match.Groups.Count > 1)
                {
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

            foreach (var pattern in typePatterns)
            {
                if (pattern.IsMatch(code))
                {
                    string patternString = pattern.ToString();
                    // 根据匹配的类型模式添加相应的命名空间
                    if (patternString.Contains("List|Dictionary|HashSet"))
                    {
                        usedTypes.Add("System.Collections.Generic");
                    }
                    else if (patternString.Contains("DataTable|DataSet"))
                    {
                        usedTypes.Add("System.Data");
                    }
                    else if (patternString.Contains("File|Directory|Path"))
                    {
                        usedTypes.Add("System.IO");
                    }
                    else if (patternString.Contains("HttpClient|WebClient"))
                    {
                        usedTypes.Add("System.Net.Http");
                    }
                    else if (patternString.Contains("JsonSerializer"))
                    {
                        usedTypes.Add("System.Text.Json");
                    }
                    else if (patternString.Contains("IEnumerable|IQueryable"))
                    {
                        usedTypes.Add("System.Linq");
                    }
                }
            }

            return usedTypes;
        }
        /// <summary>
        /// 编译C#静态类代码并返回编译结果
        /// </summary>
        /// <param name="code">要编译的C#静态类代码</param>
        /// <returns>编译结果，包含程序集和编译错误信息</returns>
        public CompilationResult CompileCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                throw new ArgumentNullException(nameof(code), "代码不能为空");
            }

            // 创建语法树
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(code);

            // 定义程序集名称
            string assemblyName = Path.GetRandomFileName();

            // 智能分析代码并获取所需的程序集引用
            var references = GetRequiredReferences(code);

            // 创建编译选项
            var compilationOptions = new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                optimizationLevel: OptimizationLevel.Debug,
                assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default);

            // 创建编译
            CSharpCompilation compilation = CSharpCompilation.Create(
                assemblyName,
                syntaxTrees: new[] { syntaxTree },
                references: references,
                options: compilationOptions);

            // 编译到内存流
            using (var ms = new MemoryStream())
            {
                EmitResult result = compilation.Emit(ms);

                if (result.Success)
                {
                    ms.Seek(0, SeekOrigin.Begin);
                    Assembly assembly = Assembly.Load(ms.ToArray());
                    return new CompilationResult { Assembly = assembly, Success = true, ErrorMessage = string.Empty };
                }
                else
                {
                    // 收集编译错误
                    var errors = new StringBuilder();
                    foreach (Diagnostic diagnostic in result.Diagnostics.Where(d => d.IsWarningAsError || d.Severity == DiagnosticSeverity.Error))
                    {
                        errors.AppendLine($"Error ({diagnostic.Id}): {diagnostic.GetMessage()} at line {diagnostic.Location.GetLineSpan().StartLinePosition.Line + 1}");
                    }
                    return new CompilationResult { Success = false, ErrorMessage = errors.ToString(), Assembly = null };
                }
            }
        }

        /// <summary>
        /// 从编译后的程序集中获取所有公共静态方法
        /// </summary>
        /// <param name="assembly">编译后的程序集</param>
        /// <returns>公共静态方法列表</returns>
        public List<MethodInfo> GetPublicStaticMethods(Assembly assembly)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly), "程序集不能为空");
            }

            var methods = new List<MethodInfo>();

            // 获取所有类型
            foreach (Type type in assembly.GetTypes())
            {
                // 获取所有公共静态方法
                MethodInfo[] typeMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
                methods.AddRange(typeMethods);
            }

            return methods;
        }

        /// <summary>
        /// 将方法转换为对应的Func委托
        /// </summary>
        /// <param name="method">要转换的方法信息</param>
        /// <returns>转换后的Func委托</returns>
        public Delegate ConvertToFuncDelegate(MethodInfo method)
        {
            if (method == null)
            {
                throw new ArgumentNullException(nameof(method), "方法信息不能为空");
            }

            // 获取方法的参数类型和返回类型
            Type[] parameterTypes = method.GetParameters().Select(p => p.ParameterType).ToArray();
            Type returnType = method.ReturnType;

            // 构建Func委托类型
            Type funcType;
            if (parameterTypes.Length == 0)
            {
                // Func<TResult>
                funcType = typeof(Func<>).MakeGenericType(returnType);
            }
            else
            {
                // Func<T1, T2, ..., TResult>
                Type[] funcTypeArgs = parameterTypes.Concat(new[] { returnType }).ToArray();
                funcType = GetFuncType(funcTypeArgs.Length);
                if (funcType == null)
                {
                    throw new NotSupportedException($"不支持超过16个参数的方法转换: {method.Name}");
                }
                funcType = funcType.MakeGenericType(funcTypeArgs);
            }

            // 创建委托
            return Delegate.CreateDelegate(funcType, method);
        }

        /// <summary>
        /// 验证生成的Func委托是否能够正确执行
        /// </summary>
        /// <param name="funcDelegate">要验证的Func委托</param>
        /// <param name="parameters">执行委托所需的参数</param>
        /// <returns>验证结果，包含执行结果和错误信息</returns>
        public ValidationResult ValidateFuncDelegate(Delegate funcDelegate, params object[] parameters)
        {
            if (funcDelegate == null)
            {
                throw new ArgumentNullException(nameof(funcDelegate), "委托不能为空");
            }

            try
            {
                // 执行委托
                object? result = funcDelegate.DynamicInvoke(parameters);
                return new ValidationResult { Success = true, Result = result, ErrorMessage = string.Empty };
            }
            catch (Exception ex)
            {
                // 处理执行异常
                string errorMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return new ValidationResult { Success = false, ErrorMessage = errorMessage, Result = null };
            }
        }

        /// <summary>
        /// 获取对应的Func委托类型
        /// </summary>
        /// <param name="genericArgumentCount">泛型参数数量（参数数量+1）</param>
        /// <returns>Func委托类型</returns>
        private Type? GetFuncType(int genericArgumentCount)
        {
            switch (genericArgumentCount)
            {
                case 1: return typeof(Func<>);
                case 2: return typeof(Func<,>);
                case 3: return typeof(Func<,,>);
                case 4: return typeof(Func<,,,>);
                case 5: return typeof(Func<,,,,>);
                case 6: return typeof(Func<,,,,,>);
                case 7: return typeof(Func<,,,,,,>);
                case 8: return typeof(Func<,,,,,,,>);
                case 9: return typeof(Func<,,,,,,,,>);
                case 10: return typeof(Func<,,,,,,,,,>);
                case 11: return typeof(Func<,,,,,,,,,,>);
                case 12: return typeof(Func<,,,,,,,,,,,>);
                case 13: return typeof(Func<,,,,,,,,,,,,>);
                case 14: return typeof(Func<,,,,,,,,,,,,,>);
                case 15: return typeof(Func<,,,,,,,,,,,,,,>);
                case 16: return typeof(Func<,,,,,,,,,,,,,,,>);
                case 17: return typeof(Func<,,,,,,,,,,,,,,,,>);
                default: return null;
            }
        }
    }

    /// <summary>
    /// 编译结果类
    /// </summary>
    public class CompilationResult
    {
        /// <summary>
        /// 编译是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 编译后的程序集
        /// </summary>
        public Assembly? Assembly { get; set; }

        /// <summary>
        /// 编译错误信息
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;
    }

    /// <summary>
    /// 验证结果类
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// 验证是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 执行结果
        /// </summary>
        public object? Result { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;
    }
}