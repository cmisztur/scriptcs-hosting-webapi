﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Common.Logging;
using ScriptCs.Contracts;

namespace ScriptCs.Hosting.WebApi
{
        public class ScriptClass
        {
            public string BaseType { get; set; }
            public string ClassName { get; set; }
        }

        public class WebApiFilePreProcessor : ScriptCs.FilePreProcessor
        {
            private readonly IFileSystem _fileSystem;
            private List<string> _sharedCode = new List<string>();
            private IList<Func<string, ScriptClass>> _classStrategies;

            public WebApiFilePreProcessor(IFileSystem fileSystem, ILog logger, IEnumerable<ILineProcessor> lineProcessors)
                : base(fileSystem, logger, lineProcessors)
            {
                _fileSystem = fileSystem;
            }

            public void SetClassStrategies(IList<Func<string, ScriptClass>> strategies)
            {
                _classStrategies = strategies;
            } 

            public void LoadSharedCode(string path)
            {
                var files = _fileSystem.EnumerateFiles(path, "*.csx", SearchOption.TopDirectoryOnly);

                _sharedCode = new List<string>();

                foreach (var file in files)
                {
                    foreach (var line in _fileSystem.ReadFileLines(file))
                    {
                        _sharedCode.Add(line);
                    }
                }
            }

            private ScriptClass GetScriptClassFromScript(string script)
            {
                var scriptClass = new ScriptClass();

                foreach (var strategy in _classStrategies)
                {
                    scriptClass = strategy(script);
                    if (scriptClass != null)
                    {
                        break;
                    }
                }

                return scriptClass;
            }

            public override void ParseScript(List<string> scriptLines, FileParserContext context)
            {
                //hack: need to change this to reference a shared binary
                scriptLines.AddRange(_sharedCode);
                var scriptClass = GetScriptClassFromScript(Path.GetFileName(context.LoadedScripts.First()));
                base.ParseScript(scriptLines, context);
                var body = context.BodyLines;
                if (scriptClass != null)
                {
                    body.Insert(0, string.Format("public class {0} : {1} {{\r\n", scriptClass.ClassName,
                                              scriptClass.BaseType));
                    body.Add("}\r\n");
                    body.Add(string.Format("typeof({0})", scriptClass.ClassName));
                }
            }

            public override FilePreProcessorResult ProcessFile(string path)
            {
                var result = base.ProcessFile(path);
                return result;
            }
        }
}
