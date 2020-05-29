using Gofive.Common.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PrintLog.Web.Helpers {
    public static class DataTableExtensions {
        private static readonly Regex EqualsRegex = new Regex(@".Equals\('([\s\S].*)?'\)|.Equals\(@(\d{1,})\)");
        private static readonly Regex ContainsRegex = new Regex(@".Contains\('([\s\S].*)?'\)|.Contains\(@(\d{1,})\)");
        private static readonly Regex StartsWithsRegex = new Regex(@".StartsWith\('([\s\S].*)?'\)|.StartsWith\(@(\d{1,})\)");
        private static readonly Regex EndsWithRegex = new Regex(@".EndsWith\('([\s\S].*)?'\)|.EndsWith\(@(\d{1,})\)");
        private static readonly Regex ParamRegex = new Regex(@"@\d{1,}");

        public static string ToDapperWhereCause(this DataTableModel model) {
            var where = model.WhereCause.Replace("&&", " AND ").Replace("||", " OR ").Replace("==", "=").Replace("== null", " IS NULL ").Replace("==null", " IS NULL ").Replace("\"", "'").Replace("= true", "= 1").Replace("= false", "= 0");
            var wheres = where.Split(new string[] { "AND", "OR" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var w in wheres) {
                var x = w.Trim();
                if (ParamRegex.IsMatch(x)) {
                    x = EqualsRegex.Replace(x, " = '@P$2'");
                    x = ContainsRegex.Replace(x, " LIKE '%'+@P$2+'%'");
                    x = StartsWithsRegex.Replace(x, " LIKE '@P$2+'%'");
                    x = EndsWithRegex.Replace(x, " LIKE '%'+@P$2'");
                    x = x.Replace("@P", "@").Replace("@", "@P");
                } else {
                    x = EqualsRegex.Replace(x, " = '$1'");
                    x = ContainsRegex.Replace(x, " LIKE '%$1%'");
                    x = StartsWithsRegex.Replace(x, " LIKE '$1%'");
                    x = EndsWithRegex.Replace(x, " LIKE '%$1'");
                }
                where = where.Replace(w, $" {x} ");
            }
            return where;
        }

        public static Dictionary<string, object> ToDapperParam(this DataTableModel model) {
            Dictionary<string, object> param = new Dictionary<string, object>();
            for (int i = 0; i < model.Parameters.Length; i++) {
                param[$"P{i}"] = model.Parameters[i];
            }
            return param;
        }

        public static string[] GetSelectableProps(this Type type) {
            var props = type.GetProperties().Where(w => !w.GetCustomAttributes().Select(s => s.GetType()).Any(a => a == typeof(NotMappedAttribute))).Where(x => x.GetAccessors()[0].IsFinal || !x.GetAccessors()[0].IsVirtual).Select(s => s.Name).ToArray();
            return props;
        }
    }
}
