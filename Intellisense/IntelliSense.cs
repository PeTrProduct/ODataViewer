﻿
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ODataViewer
{
    public class IntelliSense
    {
        #region Fields
        //private readonly EntitySource EntitySource;
        private readonly MetaData Model;

        #endregion

        public IntelliSense(MetaData Model)
        {
            this.Model = Model;
            //Tokens = new Dictionary<string, DataServiceType>();
        }
        public IntellisenseItem[] GetIntelliSense(string prefixUri)
        {
            IntellisenseItem[] result;

            if (prefixUri.IndexOf('?') == -1)
            {
                result = ResourcesIntellisense(prefixUri);
            }
            else
            {
                result = OperationIntellisense(prefixUri);
            }

            if ((result == null || result.Length == 0) &&
                 !prefixUri.EndsWith("/", StringComparison.Ordinal))
            {
                if (prefixUri.IndexOf('?') == -1)
                {
                    result = new IntellisenseItem[] { new IntellisenseItem("?", DSType.End), new IntellisenseItem("/", DSType.End) };
                }
                else
                {
                    result = new IntellisenseItem[] { new IntellisenseItem("&", DSType.End) };
                }
            }
            return result;
        }

        #region Helper Method

        #region Resource Intellisense
        private IntellisenseItem[] ResourcesIntellisense(string prefix)
        {
            string[] resources = prefix.Split('/');
            int resIndexLast = resources.Length - 1;

            #region EntitySet | EntitySet()

            if (resources.Length == 1)
            {
                Debug.WriteLine(prefix);
                return EntitySetsIntellisense(prefix);
            }

            #endregion

            #region Entity(Key)/ Property | NavigationProperties

            if (prefix.Trim().EndsWith(")/", StringComparison.Ordinal) || resources[resIndexLast - 1].EndsWith(")", StringComparison.Ordinal))
            {
                Entity et = EntityIntellisense(resources[resIndexLast - 1]);
                if (et != null)
                {
                    List<IntellisenseItem> result = new List<IntellisenseItem>();
                    result.AddRange(PropertiesIntellisense(et, resources[resIndexLast]));
                    result.AddRange(NavPropertiesIntellisense(et, resources[resIndexLast]));

                    return result.ToArray();
                }
            }

            #endregion

            #region Entity(Key)/Property/$value

            if (resources.Length >= 3)
            {
                if (resIndexLast >= 2 && resources[resIndexLast - 2].EndsWith(")", StringComparison.Ordinal))
                {
                    Entity et = EntityIntellisense(resources[resIndexLast - 2]);
                    if (et != null && et.Properties.ContainsKey(resources[resIndexLast - 1]))
                    {
                        return new IntellisenseItem[1] { new IntellisenseItem("$value", DSType.Operation) };
                    }
                }
            }

            #endregion

            return null;
        }

        private IntellisenseItem[] EntitySetsIntellisense(string prefix)
        {
            List<IntellisenseItem> result = new List<IntellisenseItem>();

            if (prefix.StartsWith("$", StringComparison.Ordinal) || string.IsNullOrEmpty(prefix))
            {
                result.Add(
                    new IntellisenseItem
                    {
                        Text = "$metadata",
                        Type = DSType.Operation,
                        ToolTip = "Get the metadata of ADO.NET Data Service."
                    });
            }

            foreach (EntitySet item in Model.Model.EntitySets.Values)
            {
                if (item.Name.StartsWith(prefix, StringComparison.CurrentCultureIgnoreCase))
                {
                    if (item.Name == prefix && !prefix.EndsWith("/", StringComparison.Ordinal))
                    {
                        result.Add(new IntellisenseItem("(", DSType.End));
                        result.Add(new IntellisenseItem("/", DSType.End));
                        result.Add(new IntellisenseItem("?", DSType.End));
                    }
                    else
                    {
                        result.Add(
                            new IntellisenseItem
                            {
                                Text = item.Name,
                                Type = DSType.EntitySet,
                                Element = item,
                                ToolTip = "This item is EntitySet"
                            });
                    }

                    //result.Add(
                    //    new IntellisenseItem
                    //    {
                    //        Text = item.Name + "(",
                    //        Type = DSType.Entity,
                    //        Element = item,
                    //        ToolTip = BuildEntityFindMethod(item.Entity)
                    //    });
                }
            }

            return result.ToArray();
        }

        private Entity EntityIntellisense(string token)
        {
            foreach (EntitySet item in Model.Model.EntitySets.Values)
            {
                if (token.StartsWith(item.Name, StringComparison.CurrentCultureIgnoreCase))
                {
                    if (token.EndsWith(")", StringComparison.CurrentCultureIgnoreCase))
                    {
                        return item.Entity;
                    }
                }
            }
            return null;
        }

        private IntellisenseItem[] PropertiesIntellisense(Entity et, string prefix)
        {
            List<IntellisenseItem> result = new List<IntellisenseItem>();

            if (et == null)
            {
                return result.ToArray();
            }

            foreach (string p in et.Properties.Keys)
            {
                if (p.StartsWith(prefix, StringComparison.CurrentCultureIgnoreCase))
                {
                    if (p == prefix)
                    {
                        result.Insert(0, new IntellisenseItem("/", DSType.End));
                        result.Insert(0, new IntellisenseItem("?", DSType.End));
                        result.Insert(0, new IntellisenseItem("&", DSType.End));
                        result.Insert(0, new IntellisenseItem(",", DSType.End));
                    }
                    else
                    {
                        result.Add(
                            new IntellisenseItem
                            {
                                Text = p,
                                Element = et,
                                Type = DSType.Property,
                                ToolTip = BuildPropertyToolTip(et, p)
                            });
                    }
                }
            }

            return result.ToArray();
        }

        private IntellisenseItem[] NavPropertiesIntellisense(Entity et, string prefix)
        {
            List<IntellisenseItem> result = new List<IntellisenseItem>();

            if (et == null)
            {
                return result.ToArray();
            }

            foreach (string n in et.NavigationProperties.Keys)
            {
                if (n.StartsWith(prefix, StringComparison.CurrentCultureIgnoreCase))
                {
                    if (n == prefix)
                    {
                        result.Insert(0, new IntellisenseItem("/", DSType.End));
                        result.Insert(0, new IntellisenseItem("?", DSType.End));
                    }
                    else
                    {
                        result.Add(
                            new IntellisenseItem
                            {
                                Text = n,
                                Element = et,
                                Type = DSType.NavigationProperty,
                                ToolTip = "Navigation Property"
                            });
                    }
                    //result.Add(
                    //    new IntellisenseItem
                    //    {
                    //        Text = n + "(",
                    //        Element = et,
                    //        Type = DSType.NavigationProperty,
                    //        ToolTip = BuildEntityFindMethod( et.NavigationProperties[n] )
                    //    });
                }
            }

            return result.ToArray();
        }
        #endregion

        #region Sentence
        private IntellisenseItem[] OperationIntellisense(string prefix)
        {
            #region Init Fields
            string resources = prefix.Split('?').First();
            string serfix = prefix.Split('?').Last();
            bool IsEntitySet = true;
            string lastResource = resources.Split('/').Last();
            string lastExp = serfix.Split('&').Last();
            string lastOperation = lastExp.Split('=').First();
            EntitySet es = null;
            Entity et = null;

            if (lastResource.IndexOf('(') != -1)
            {
                lastResource = lastResource.Substring(0, lastResource.IndexOf('('));
                IsEntitySet = false;
            }

            if (Model.Model.EntitySets.ContainsKey(lastResource))
            {
                es = Model.Model.EntitySets[lastResource];
            }

            if (es != null)
            {
                et = es.Entity;
            }
            #endregion

            List<IntellisenseItem> result = new List<IntellisenseItem>();

            #region $expand
            if ("$expand=".StartsWith(lastOperation, StringComparison.CurrentCultureIgnoreCase))
            {
                string serfixExpand = lastExp.Split(',').Last();
                string navProp = serfixExpand.Split('=').Last().Trim();
                if (lastExp.Split('=').First() == "$expand")
                {
                    if (et.NavigationProperties.ContainsKey(navProp))
                    {
                        result.Add(new IntellisenseItem(",", DSType.End));
                        result.Add(new IntellisenseItem("&", DSType.End));
                    }
                    else
                    {
                        result.AddRange(NavPropertiesIntellisense(et, navProp));
                    }
                }
                else
                {
                    result.Add(new IntellisenseItem("$expand=", DSType.Operation, "The $expand= option allows you to embed one or more sets of related entities in the results."));
                }
            }
            #endregion

            #region $top
            if (IsEntitySet && "$top=".StartsWith(lastExp, StringComparison.CurrentCultureIgnoreCase) && lastExp.Trim() != "$top=")
            {
                result.Add(new IntellisenseItem("$top=", DSType.Operation, "Restrict the maximum number of entities to be returned."));
            }
            #endregion

            #region $skip
            if (IsEntitySet && "$skip=".StartsWith(lastExp, StringComparison.CurrentCultureIgnoreCase) && lastExp.Trim() != "$skip=")
            {
                result.Add(new IntellisenseItem("$skip=", DSType.Operation, "Skip the number of rows given in this parameter when returning results."));
            }
            #endregion

            #region $orderby
            if (IsEntitySet && "$orderby=".StartsWith(lastOperation, StringComparison.CurrentCultureIgnoreCase))
            {

                if (lastExp.Trim().StartsWith("$orderby=", StringComparison.OrdinalIgnoreCase))
                {
                    string prop = lastExp.Split('=').Last();

                    if (et.Properties.ContainsKey(prop.Trim()) && prop.EndsWith(" ", StringComparison.Ordinal))
                    {
                        result.Add(new IntellisenseItem("desc", DSType.Operation, "Sort the results by the criteria given in this value."));
                    }

                    result.AddRange(PropertiesIntellisense(et, lastExp.Split('=').Last()));
                }
                else
                {
                    result.Add(new IntellisenseItem("$orderby=", DSType.Operation, "Sort the results by the criteria given in this value."));
                }
            }
            #endregion

            #region $select
            if (IsEntitySet && "$select=".StartsWith(lastOperation, StringComparison.CurrentCultureIgnoreCase))
            {

                if (lastExp.Trim().StartsWith("$select=", StringComparison.OrdinalIgnoreCase))
                {
                    result.AddRange(PropertiesIntellisense(et, lastExp.Split('=',',').Last()));
                }
                else
                {
                    result.Add(new IntellisenseItem("$select=", DSType.Operation, "Selects a subset of properties to include in the response."));
                }
            }
            #endregion

            #region $filter

            if (IsEntitySet && "$filter=".StartsWith(lastExp, StringComparison.CurrentCultureIgnoreCase) &&
                 "$filter=" != lastExp)
            {
                result.Add(new IntellisenseItem("$filter=", DSType.Operation, "Restrict the entities returned from a query by applying the expression specified\n in this operator to the entity set identified by the last segment of the URI path."));
            }
            if (IsEntitySet && lastExp.StartsWith("$filter=", StringComparison.CurrentCultureIgnoreCase))
            {
                IntellisenseItem[] f = FilterIntellisense(lastResource, lastExp.Split('=').Last(), et);
                if (f != null)
                {
                    result.AddRange(f);
                }
            }
            #endregion

            return result.ToArray();
        }

        private IntellisenseItem[] FilterIntellisense(string lastResource, string exp, Entity et)
        {
            string rightExp = null;
            string op = null;
            string leftExp = null;

            #region Init Fields
            exp = exp.TrimStart();

            List<string> groups = exp.Split(new[] { " and ", " or ", " not " }, StringSplitOptions.None).ToList();
            if (groups.Count > 0)
            {
                exp = groups.Last();
            }

            if (exp.IndexOf(" ", StringComparison.Ordinal) == -1)
            {
                leftExp = exp;
                op = string.Empty;
                rightExp = null;
            }
            else
            {
                leftExp = exp.Substring(0, exp.IndexOf(" ", StringComparison.Ordinal));
                if (exp.IndexOf(" ", leftExp.Length + 1, StringComparison.Ordinal) != -1)
                {
                    int lengthOp = exp.IndexOf(" ", leftExp.Length + 1, StringComparison.Ordinal) - leftExp.Length + 1;
                    op = exp.Substring(leftExp.Length, lengthOp);

                    if (op.Length >= 2)
                    {
                        if (leftExp.Length + op.Length < exp.Length)
                        {
                            rightExp = exp.Substring(leftExp.Length + op.Length, exp.Length - leftExp.Length - op.Length);
                        }
                    }
                }
            }
            #endregion

            string[] tokens = exp.Split(' ');

            // Left side of expression
            if (string.IsNullOrEmpty(op) && !exp.EndsWith(" ", StringComparison.Ordinal))
            {
                List<IntellisenseItem> result = new List<IntellisenseItem>();

                if (leftExp.IndexOf('/') != -1)
                {
                    string[] t = leftExp.Split('/');
                    string res = t[t.Length - 2].Split('=').Last();
                    Entity etsub = et.NavigationProperties[res];

                    result.AddRange(PropertiesIntellisense(etsub, t.Last()));
                    result.AddRange(NavPropertiesIntellisense(etsub, t.Last()));
                }
                else
                {
                    result.AddRange(PropertiesIntellisense(et, exp.Trim()));
                    result.AddRange(NavPropertiesIntellisense(et, exp.Trim()));
                    result.AddRange(Expression.BoolFunc);
                }

                return result.ToArray();
            }

            // The group operators (and/or/not)
            if (!string.IsNullOrEmpty(leftExp)
                && !string.IsNullOrEmpty(op)
                && !string.IsNullOrEmpty(rightExp)
                && exp.EndsWith(" ", StringComparison.Ordinal)
                )
            {
              return Expression.LogicalGroupOperators;
            }

            // The operations (eq/ne/gt/ge/lt/le)
            if (string.IsNullOrEmpty(op) && exp.EndsWith(" ", StringComparison.Ordinal))
            {
                return Expression.LogicalOperators;
            }

            // Right side of expression
            if (!string.IsNullOrEmpty(op) && exp.EndsWith(" ", StringComparison.Ordinal))
            {
                string type = null;
                // Field
                if (et.Properties.ContainsKey(leftExp))
                {
                    List<IntellisenseItem> result = new List<IntellisenseItem>();

                    type = et.Properties[leftExp].NameType.Split('.').Last();
                    result.AddRange(Expression.Funcs(type).ToArray());
                    result.AddRange(from x in et.Properties.Values
                                    where x.NameType.Split('.').Last() == type
                                    select new IntellisenseItem(x.Name, DSType.Property));

                    return result.ToArray();
                }
            }


            return null;
        }
        #endregion

        #region ToolTip Methods
        private string BuildEntityFindMethod(Entity et)
        {
            if (et == null)
            {
                return string.Empty;
            }

            string result = et.Name;
            string[] keys = et.Keys.Keys.ToArray();
            result += "( ";

            for (int i = 0; i < keys.Length; i++)
            {
                result += et.Properties[keys[i]].NameType.Split('.').Last() + " " + keys[i] + ", ";
            }

            result = result.Substring(0, result.LastIndexOf(','));

            result += " )";

            return result;


        }

        private string BuildPropertyToolTip(Entity et, string property)
        {
            return
                $"This item is property of {et.Name} Entity.\n{property} type of {et.Properties[property].NameType.Split('.').Last()}. ";
        }
        #endregion

        #endregion
    }
}
