using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityModManagerNet;

namespace Iridium
{
    public class Logger
    {
        private readonly UnityModManager.ModEntry.ModLogger _logger;

        // 定义颜色常量
        private const string COLOR_RESET = "</color>";
        private const string COLOR_KEYWORD = "<color=#569CD6>"; // 例如：class, struct
        private const string COLOR_STRING = "<color=#CE9178>"; // 字符串
        private const string COLOR_NUMBER = "<color=#B5CEA8>"; // 数字
        private const string COLOR_BOOLEAN = "<color=#569CD6>"; // 布尔值
        private const string COLOR_NULL = "<color=#569CD6>"; // null
        private const string COLOR_OBJECT = "<color=#9CDCFE>"; // 对象名/类型名
        private const string COLOR_PROPERTY_NAME = "<color=#9CDCFE>"; // 对象属性名
        private const string COLOR_BRACKET = "<color=#FFD700>"; // 括号 [] {}
        private const string COLOR_COMMENT = "<color=#6A9955>"; // 注释或额外信息
        private const string COLOR_ERROR = "<color=#F44747>"; // 错误信息
        private const string COLOR_TYPE_NAME = "<color=#4EC9B0>"; // 类型名称

        // 格式化限制
        private const int MAX_DEPTH = 5; // 最大递归深度
        private const int MAX_ARRAY_ELEMENTS = 100; // 数组最大显示元素数量
        private const int MAX_OBJECT_PROPERTIES = 50; // 对象最大显示属性数量

        public Logger(UnityModManager.ModEntry.ModLogger logger)
        {
            _logger = logger;
        }

        // Log 支持多参数和对象展开
        public void Log(params object[] args)
        {
            var message = ConvertArgsToString(args);
            _logger.Log(message);
        }

        // Error 映射到LogException
        public void Error(params object[] args)
        {
            var message = ConvertArgsToString(args);
            _logger.LogException("Mod_Error", new Exception(message));
        }

        // Warning 映射到Warning
        public void Warning(params object[] args)
        {
            var message = ConvertArgsToString(args);
            _logger.Warning(message);
        }

        // Dir 用于显示对象的属性列表
        public void Dir(object obj)
        {
            var message = "Object:\n" + FormatObject(obj, 0);
            _logger.Log(message);
        }

        // Debug 等同于Log
        public void Debug(params object[] args) => Log(args);

        // Info 等同于Log
        public void Info(params object[] args) => Log(args);

        // 转换参数为字符串，支持对象展开
        private string ConvertArgsToString(object[] args)
        {
            if (args == null || args.Length == 0)
                return string.Empty;

            return string.Join(" ", args.Select(arg => FormatValue(arg)));
        }

        // 格式化单个值，区分普通值和对象
        private string FormatValue(object value, int depth = 0)
        {
            if (value == null)
                return $"{COLOR_NULL}null{COLOR_RESET}";

            if (value is string str)
                return $"{COLOR_STRING}\"{EscapeString(str)}\"{COLOR_RESET}";
            if (value is bool b)
                return $"{COLOR_BOOLEAN}{b.ToString().ToLower()}{COLOR_RESET}";
            if (value.GetType().IsPrimitive || value is decimal)
                return $"{COLOR_NUMBER}{value}{COLOR_RESET}";
            if (value.GetType().IsEnum)
                return $"{COLOR_KEYWORD}{value.GetType().Name}{COLOR_RESET}.{COLOR_PROPERTY_NAME}{value}{COLOR_RESET}";

            if (value is IEnumerable enumerable)
                return FormatEnumerable(enumerable, depth);

            return FormatObject(value, depth);
        }

        private string FormatEnumerable(IEnumerable enumerable, int depth)
        {
            var list = new List<object>();
            foreach (var item in enumerable)
            {
                list.Add(item);
            }

            if (list.Count == 0)
                return $"{COLOR_TYPE_NAME}Array{COLOR_RESET}{COLOR_BRACKET}[{COLOR_RESET}]";

            if (depth >= MAX_DEPTH)
                return $"{COLOR_TYPE_NAME}Array({list.Count}){COLOR_RESET}{COLOR_BRACKET}[...]{COLOR_RESET}";

            var indent = GetIndent(depth);
            var nextIndent = GetIndent(depth + 1);

            var items = list.Take(MAX_ARRAY_ELEMENTS)
                            .Select(item => FormatValue(item, depth + 1))
                            .ToList();

            if (list.Count > MAX_ARRAY_ELEMENTS)
            {
                items.Add($"{COLOR_COMMENT}... {list.Count - MAX_ARRAY_ELEMENTS} more ...{COLOR_RESET}");
            }

            return $"{COLOR_TYPE_NAME}Array({list.Count}){COLOR_RESET} {COLOR_BRACKET}[{COLOR_RESET}\n{nextIndent}{string.Join($",\n{nextIndent}", items)}\n{indent}{COLOR_BRACKET}]{COLOR_RESET}";
        }

        private string FormatObject(object obj, int depth)
        {
            if (obj == null)
                return $"{COLOR_NULL}null{COLOR_RESET}";

            string typeName = obj.GetType().Name;

            if (depth >= MAX_DEPTH)
                return $"{COLOR_TYPE_NAME}{typeName}{COLOR_RESET} {COLOR_BRACKET}{{{COLOR_RESET}...{COLOR_BRACKET}}}{COLOR_RESET}";

            var indent = GetIndent(depth);
            var nextIndent = GetIndent(depth + 1);

            var propStrings = new List<string>();

            var properties = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                            .Where(p => p.CanRead)
                            .OrderBy(p => p.Name)
                            .ToList();

            var fields = obj.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance)
                            .OrderBy(f => f.Name)
                            .ToList();

            for (int i = 0; i < properties.Count; i++)
            {
                if (propStrings.Count >= MAX_OBJECT_PROPERTIES) break;

                var prop = properties[i];
                try
                {
                    var value = prop.GetValue(obj);
                    propStrings.Add($"{COLOR_PROPERTY_NAME}{prop.Name}{COLOR_RESET}: {FormatValue(value, depth + 1)}");
                }
                catch (Exception ex)
                {
                    propStrings.Add($"{COLOR_PROPERTY_NAME}{prop.Name}{COLOR_RESET}: {COLOR_ERROR}[Error: {ex.Message}]{COLOR_RESET}");
                }
            }

            for (int i = 0; i < fields.Count; i++)
            {
                if (propStrings.Count >= MAX_OBJECT_PROPERTIES) break;

                var field = fields[i];
                try
                {
                    var value = field.GetValue(obj);
                    propStrings.Add($"{COLOR_PROPERTY_NAME}{field.Name}{COLOR_RESET}: {FormatValue(value, depth + 1)}");
                }
                catch (Exception ex)
                {
                    propStrings.Add($"{COLOR_PROPERTY_NAME}{field.Name}{COLOR_RESET}: {COLOR_ERROR}[Error: {ex.Message}]{COLOR_RESET}");
                }
            }

            if (properties.Count + fields.Count > MAX_OBJECT_PROPERTIES)
            {
                propStrings.Add($"{COLOR_COMMENT}... more properties/fields ...{COLOR_RESET}");
            }

            if (!propStrings.Any())
                return $"{COLOR_TYPE_NAME}{typeName}{COLOR_RESET} {COLOR_BRACKET}{{{COLOR_RESET}{COLOR_BRACKET}}}{COLOR_RESET}";

            return $"{COLOR_TYPE_NAME}{typeName}{COLOR_RESET} {COLOR_BRACKET}{{{COLOR_RESET}\n{nextIndent}{string.Join($",\n{nextIndent}", propStrings)}\n{indent}{COLOR_BRACKET}}}{COLOR_RESET}";
        }

        private string GetIndent(int depth)
        {
            return new string(' ', depth * 4);
        }

        private string EscapeString(string str)
        {
            return str.Replace("\"", "\\\"")
                      .Replace("\n", "\\n")
                      .Replace("\r", "\\r")
                      .Replace("\t", "\\t");
        }
    }
}