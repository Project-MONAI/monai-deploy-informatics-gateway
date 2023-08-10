/*
 * Copyright 2023 MONAI Consortium
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.IO.Abstractions;
using System.Xml;

namespace Monai.Deploy.InformaticsGateway.CLI.Services
{
    public interface INLogConfigurationOptionAccessor
    {
        /// <summary>
        /// Gets the logs directory path.
        /// </summary>
        string LogStoragePath { get; }
    }

    public class NLogConfigurationOptionAccessor : INLogConfigurationOptionAccessor
    {
        private readonly XmlDocument _xmlDocument;
        private readonly XmlNamespaceManager _namespaceManager;

        public NLogConfigurationOptionAccessor(IFileSystem fileSystem)
        {
            if (fileSystem is null)
            {
                throw new ArgumentNullException(nameof(fileSystem));
            }

            var xml = fileSystem.File.ReadAllText(Common.NLogConfigFilePath);
            _xmlDocument = new XmlDocument();
            _xmlDocument.LoadXml(xml);
            _namespaceManager = new XmlNamespaceManager(_xmlDocument.NameTable);
            _namespaceManager.AddNamespace("ns", "http://www.nlog-project.org/schemas/NLog.xsd");
        }

        public string LogStoragePath
        {
            get
            {
                var value = _xmlDocument.SelectSingleNode("//ns:variable[@name='logDir']/@value", _namespaceManager).InnerText;
                value = value.Replace("${basedir}", Common.ContainerApplicationRootPath);
                return value;
            }
        }
    }
}
