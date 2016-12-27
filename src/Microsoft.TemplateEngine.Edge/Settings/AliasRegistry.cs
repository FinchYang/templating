﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Utils;
using Newtonsoft.Json.Linq;

namespace Microsoft.TemplateEngine.Edge.Settings
{
    public static class AliasRegistry
    {
        private static JObject _source;
        private static readonly Dictionary<string, string> AliasesToTemplates = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, string> TemplatesToAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private static void Load()
        {
            if (TemplatesToAliases.Count > 0)
            {
                return;
            }

            if (!Paths.User.AliasesFile.Exists())
            {
                _source = new JObject();
                return;
            }

            string sourcesText = Paths.User.AliasesFile.ReadAllText("{}");
            _source = JObject.Parse(sourcesText);

            foreach (JProperty child in _source.Properties())
            {
                AliasesToTemplates[child.Name] = child.Value.ToString();
                TemplatesToAliases[child.Value.ToString()] = child.Name;
            }
        }

        public static string GetTemplateNameForAlias(string alias)
        {
            if(alias == null)
            {
                return null;
            }

            Load();
            string templateName;
            if (AliasesToTemplates.TryGetValue(alias, out templateName))
            {
                return templateName;
            }

            return null;
        }

        public static IReadOnlyDictionary<string, ITemplateInfo> GetTemplatesForAlias(string alias, IReadOnlyCollection<ITemplateInfo> templates)
        {
            Dictionary<string, ITemplateInfo> aliasVsTemplate = new Dictionary<string, ITemplateInfo>();

            if(alias == null)
            {
                return aliasVsTemplate;
            }

            Load();
            string templateName;
            if(AliasesToTemplates.TryGetValue(alias, out templateName))
            {
                ITemplateInfo match = templates.FirstOrDefault(x => string.Equals(x.Name, templateName, StringComparison.OrdinalIgnoreCase));

                if (match != null)
                {
                    aliasVsTemplate[alias] = match;
                    return aliasVsTemplate;
                }
            }

            if (!string.IsNullOrWhiteSpace(alias))
            {
                Dictionary<string, string> matchedAliases = AliasesToTemplates.Where(x => x.Key.IndexOf(alias, StringComparison.OrdinalIgnoreCase) > -1).ToDictionary(x => x.Value, x => x.Key);

                foreach (ITemplateInfo template in templates)
                {
                    if (matchedAliases.TryGetValue(template.Name, out string matchingAlias))
                    {
                        aliasVsTemplate[matchingAlias] = template;
                    }
                }
            }

            return aliasVsTemplate;
        }

        public static string GetAliasForTemplate(ITemplateInfo template)
        {
            Load();
            string alias;
            if(!TemplatesToAliases.TryGetValue(template.Name, out alias))
            {
                return null;
            }

            return alias;
        }

        // returns -1 if the alias already exists, zero otherwise
        public static int SetTemplateAlias(string alias, ITemplateInfo template)
        {
            Load();
            if (AliasesToTemplates.ContainsKey(alias))
            {
                return -1;
            }

            _source[alias] = template.Name;
            EngineEnvironmentSettings.Host.FileSystem.WriteAllText(Paths.User.AliasesFile, _source.ToString());
            return 0;
        }
    }
}
