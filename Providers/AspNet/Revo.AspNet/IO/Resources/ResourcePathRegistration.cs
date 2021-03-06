﻿using System.Collections.Generic;

namespace Revo.AspNet.IO.Resources
{
    public class ResourcePathRegistration
    {
        public string AssemblyName { get; set; }
        public string ProjectSourcePath { get; set; }
        public Dictionary<string, string> PathMappings { get; set; } = new Dictionary<string, string>();
    }
}
