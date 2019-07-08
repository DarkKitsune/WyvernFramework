using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace WyvernFramework
{
    /// <summary>
    /// An interface for objects that contain useful debug information
    /// </summary>
    public interface IDebug
    {
        string Name { get; }
        string Description { get; }
    }

    /// <summary>
    /// Extensions for debugging
    /// </summary>
    public static class DebugExtensions
    {
        /// <summary>
        /// Get strings containing debug info for the object
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="recursive">Whether ToStringDebug should be used for property/field values as well</param>
        /// <returns></returns>
        public static IEnumerable<string> ToStringDebug(this object obj)
        {
            // Just return "null" if null
            if (obj is null)
            {
                yield return "null";
                yield break;
            }
            // Get object type
            var type = obj.GetType();
            // Get properties
            var properties = type.GetProperties(
                    BindingFlags.Public
                    | BindingFlags.NonPublic
                    | BindingFlags.Instance
                    | BindingFlags.FlattenHierarchy
                ).Where(e => !(e.GetMethod is null));
            // Get fields
            var fields = type.GetFields(
                    BindingFlags.Public
                    | BindingFlags.NonPublic
                    | BindingFlags.Instance
                    | BindingFlags.FlattenHierarchy
                );
            // If doesn't have fields or properties then use a simpler print
            if (fields.Length == 0 && !properties.Any())
            {
                // Use extra information if obj implements IDebug
                if (obj is IDebug _debug)
                {
                    var name = _debug.Name ?? "";
                    yield return $"{type} \"{name}\" (ToString()=\"{_debug}\")";
                    if (!string.IsNullOrEmpty(_debug.Description))
                    {
                        yield return $"({_debug.Description})";
                    }
                }
                // obj does not implement IDebug so just use the type and ToString() value
                else
                {
                    yield return $"{type} (ToString()=\"{obj}\")";
                }
                yield break;
            }
            // Use extra information if obj implements IDebug
            if (obj is IDebug debug)
            {
                var name = debug.Name ?? "";
                yield return $"{type} \"{name}\" (ToString()=\"{debug}\")";
                if (!string.IsNullOrEmpty(debug.Description))
                {
                    yield return $"({debug.Description})";
                }
            }
            // obj does not implement IDebug so just use the type and ToString() value
            else
            {
                yield return $"{type} (ToString()=\"{obj}\")";
            }
            yield return "{";
            // Print out property values
            foreach (var property in properties)
            {
                var value = property.GetValue(obj);
                if (value is IEnumerable<object> enumerable)
                    yield return $"    {property.Name}: {{ {string.Join(", ", enumerable)} }}";
                else
                    yield return $"    {property.Name}: {value}";
            }
            // Print out field values
            foreach (var field in fields)
            {
                var value = field.GetValue(obj);
                if (value is IEnumerable<object> enumerable)
                    yield return $"    {field.Name}: {{ {string.Join(", ", enumerable)} }}";
                else
                    yield return $"    {field.Name}: {value}";
            }
            yield return "}";
        }

        /// <summary>
        /// Print debug info for the object to the console
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static void PrintDebug(this object obj)
        {
            Debug.Info(string.Join("\n", obj.ToStringDebug()), "Debug");
        }
    }

    /// <summary>
    /// Class providing debug messages and such
    /// </summary>
    public static class Debug
    {
        public static ConsoleColor ErrorBackground = ConsoleColor.DarkRed;
        public static ConsoleColor ErrorForeground = ConsoleColor.White;
        public static ConsoleColor WarningBackground = ConsoleColor.DarkYellow;
        public static ConsoleColor WarningForeground = ConsoleColor.White;
        public static ConsoleColor InfoBackground = ConsoleColor.DarkBlue;
        public static ConsoleColor InfoForeground = ConsoleColor.White;

        /// <summary>
        /// Write a line of text to the console
        /// </summary>
        /// <param name="text"></param>
        /// <param name="prefix"></param>
        public static void WriteLine(string text, string prefix = null)
        {
            if (!string.IsNullOrEmpty(prefix))
                Console.Write($"[{prefix}] ");
            Console.WriteLine(text);
        }

        /// <summary>
        /// Write a line of text to the console with color
        /// </summary>
        /// <param name="text"></param>
        /// <param name="prefix"></param>
        public static void WriteLine(ConsoleColor backColor, ConsoleColor foreColor, string text, string prefix = null)
        {
            // Store old background colors and set new ones
            var oldBack = Console.BackgroundColor;
            var oldFore = Console.ForegroundColor;
            Console.BackgroundColor = backColor;
            Console.ForegroundColor = foreColor;
            // Write the message
            WriteLine(text, prefix);
            //  Restore the old background colors
            Console.BackgroundColor = oldBack;
            Console.ForegroundColor = oldFore;
        }

        /// <summary>
        /// Write some text to the console
        /// </summary>
        /// <param name="text"></param>
        /// <param name="prefix"></param>
        public static void Write(string text, string prefix = null)
        {
            if (!string.IsNullOrEmpty(prefix))
                Console.Write($"[{prefix}] ");
            Console.Write(text);
        }

        /// <summary>
        /// Write some text to the console with color
        /// </summary>
        /// <param name="text"></param>
        /// <param name="prefix"></param>
        public static void Write(ConsoleColor backColor, ConsoleColor foreColor, string text, string prefix = null)
        {
            // Store old background colors and set new ones
            var oldBack = Console.BackgroundColor;
            var oldFore = Console.ForegroundColor;
            Console.BackgroundColor = backColor;
            Console.ForegroundColor = foreColor;
            // Write the message
            Write(text, prefix);
            //  Restore the old background colors
            Console.BackgroundColor = oldBack;
            Console.ForegroundColor = oldFore;
        }

        /// <summary>
        /// Write an info message
        /// </summary>
        /// <param name="text"></param>
        /// <param name="origin"></param>
        public static void Info(string text, string origin = null)
        {
            if (string.IsNullOrEmpty(origin))
                origin = "Info";
            else
                origin += " (Info)";
            WriteLine(InfoBackground, InfoForeground, text, origin);
        }

        /// <summary>
        /// Write a warning message
        /// </summary>
        /// <param name="text"></param>
        /// <param name="origin"></param>
        public static void Warning(string text, string origin = null)
        {
            if (string.IsNullOrEmpty(origin))
                origin = "Warning";
            else
                origin += " (Warning)";
            WriteLine(WarningBackground, WarningForeground, text, origin);
        }

        /// <summary>
        /// Write an error message
        /// </summary>
        /// <param name="text"></param>
        /// <param name="origin"></param>
        public static void Error(string text, string origin = null)
        {
            if (string.IsNullOrEmpty(origin))
                origin = "Error";
            else
                origin += " (Error)";
            WriteLine(ErrorBackground, ErrorForeground, text, origin);
        }
    }
}
