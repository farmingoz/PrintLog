using Gofive.Common.Core;
using Gofive.Common.Core.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace PrintLog.Web.Models {
    public class DataTableRequestModel<T> {
        private const string DATETIMEFORMAT = "yyyy-MM-dd HH:mm:ss";
        private const string DATEFORMAT = "yyyy-MM-dd";
        public string WhereCause { get; set; }
        public string OrderBy { get; set; }
        public int Skip { get; set; } = 0;
        public int Take { get; set; } = 10;
        public string Search { get; set; }
        public object Filter { get; set; }
        internal string[] Filters { get; set; }
        public void SetFilters(string[] filters) {
            this.Filters = filters;
        }

        public DataTableModel ToDataTableModel(string[] filters = null) {
            string whereCause;
            object[] parameters;
            ParseWhereCause(out whereCause, out parameters);
            if (filters?.Any() == true) {
                whereCause = $"({string.Join(" && ", filters)}) && " + whereCause;
            } else if (Filters?.Any() == true) {
                whereCause = $"({string.Join(" && ", Filters)}) && " + whereCause;
            }
            var model = new DataTableModel {
                OrderBy = OrderBy,
                Parameters = parameters,
                Skip = Skip,
                Take = Take,
                WhereCause = whereCause,
            };
            if (model.Take == 0) model.Take = 10;
            return model;
        }

        private void ParseWhereCause(out string whereCause, out object[] parameters) {
            if (string.IsNullOrWhiteSpace(WhereCause)) {
                whereCause = "1=0";
                parameters = new object[0];
                return;
            };
            var json = JObject.Parse(WhereCause);
            var columns = new List<DTColumn>();
            var props = typeof(T).GetProperties();
            var param = new DTParameters {
                Search = new DTSearch()
            };
            foreach (var token in json) {
                var prop = props.FirstOrDefault(a => a.Name.ToLower() == token.Key.ToLower());
                if (prop != null) {
                    columns.Add(new DTColumn {
                        Data = prop.Name,
                        Name = prop.Name,
                        Searchable = true,
                        Search = new DTSearch {
                            Value = token.Value.ToString()
                        }
                    });
                }
            }
            param.Columns = columns.ToArray();
            param.CreateWhereCause<T>(out whereCause, out parameters);
            List<string> wheres = new List<string>();
            if (json.ContainsKey("dtFilter")) {
                var dtFilter = BuildWhereCause((JObject)json["dtFilter"], props, " && ");
                if (!string.IsNullOrWhiteSpace(dtFilter)) wheres.Add(dtFilter);
            }
            if (json.ContainsKey("dtSearch")) {
                var dtSearch = BuildWhereCause((JObject)json["dtSearch"], props, " || ");
                if (!string.IsNullOrWhiteSpace(dtSearch)) wheres.Add(dtSearch);
            }
            if (wheres.Any()) {
                whereCause += $" && ({string.Join(" && ", wheres.Select(s => $"({s})"))})";
            }
        }

        private string BuildDateFilter(string dateString, string propName, string @operator, CultureInfo cultureInfo) {
            DateTime date;
            if (DateTime.TryParseExact(dateString, DATETIMEFORMAT, cultureInfo, DateTimeStyles.None, out date)) {
                return ($"{propName} {@operator} '{date.ToString(DATETIMEFORMAT, cultureInfo)}'");
            } else if (DateTime.TryParseExact(dateString, DATEFORMAT, cultureInfo, DateTimeStyles.None, out date)) {
                return ($"{propName} {@operator} '{date.ToString(DATEFORMAT, cultureInfo)}'");
            }
            return null;
        }


        private string BuildWhereCause(JObject searchs, PropertyInfo[] props, string searchOperation) {
            var searchKeys = searchs.Properties().ToDictionary(k => k.Name.ToLower(), v => v.Name);
            List<string> searchCauses = new List<string>();
            var validProps = props.Where(w => !w.CustomAttributes.Any(a => a.AttributeType.Name == "NotMappedAttribute") && searchKeys.ContainsKey(w.Name.ToLower())).ToList();
            CultureInfo cultureInfo = null;
            foreach (var item in searchKeys) {
                var value = searchs[item.Value].ToString().Trim();
                decimal dec;
                if (string.IsNullOrEmpty(value)) continue;
                var prop = validProps.FirstOrDefault(a => a.Name.ToLower() == item.Key);
                if (prop != null) {
                    var propType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                    bool isPrimitive = propType.IsPrimitive;
                    if (propType == typeof(DateTime)) {
                        if (cultureInfo == null) cultureInfo = CultureInfo.GetCultureInfo("en-GB");
                        if (value.StartsWith("[")) {
                            var dateStrings = JArray.Parse(value).ToObject<string[]>();
                            if (dateStrings.Length > 1) {
                                var date1 = BuildDateFilter(dateStrings[0], prop.Name, ">=", cultureInfo);
                                var date2 = BuildDateFilter(dateStrings[1], prop.Name, "<=", cultureInfo);
                                if (date1 != null && date2 != null) searchCauses.Add($"({date1} && {date2})");
                            } else {
                                var filterDate = BuildDateFilter(dateStrings[0], prop.Name, "==", cultureInfo);
                                if (filterDate == null) searchCauses.Add(filterDate);
                            }
                        } else {
                            var filterDate = BuildDateFilter(value, prop.Name, "==", cultureInfo);
                            if (filterDate == null) searchCauses.Add(filterDate);
                        }
                    } else if (prop.PropertyType == typeof(string) && prop.Name.EndsWith("Id")) {
                        if (value.Contains(",")) {
                            var vals = value.Split(',', StringSplitOptions.RemoveEmptyEntries);
                            searchCauses.Add("(" + string.Join(" || ", vals.Select(val => $"{prop.Name} == \"{val}\"")) + ")");
                        } else {
                            searchCauses.Add($"{prop.Name} == \"{value}\"");
                        }
                    } else if (propType == typeof(string)) {
                        searchCauses.Add($"{prop.Name}.Contains(\"{value}\")");
                    } else if (isPrimitive && value.Contains(",")) {
                        var vals = value.Split(',', StringSplitOptions.RemoveEmptyEntries);
                        var temps = new List<string>();
                        foreach (var val in vals) {
                            if (decimal.TryParse(val, out dec)) {
                                temps.Add($"{prop.Name} == {dec}");
                            } else if (bool.TryParse(val, out bool flag)) {
                                temps.Add($"{prop.Name} == {(flag ? 1 : 0)}");
                            }
                        }
                        searchCauses.Add("(" + string.Join(" || ", temps) + ")");
                    } else if (isPrimitive && decimal.TryParse(value, out dec)) {
                        searchCauses.Add($"{prop.Name} == {dec}");
                    } else if (isPrimitive && bool.TryParse(value, out bool flag)) {
                        searchCauses.Add($"{prop.Name} == {(flag ? 1 : 0)}");
                    }
                }
            }
            return string.Join(searchOperation, searchCauses);
        }
    }
}
