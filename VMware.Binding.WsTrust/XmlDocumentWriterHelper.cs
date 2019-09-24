/*
 ********************************************************
 * Copyright (c) VMware, Inc. 2016. All Rights Reserved.
 ********************************************************
 *
 * DISCLAIMER. THIS PROGRAM IS PROVIDED TO YOU "AS IS" WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, WHETHER ORAL OR WRITTEN,
 * EXPRESS OR IMPLIED. THE AUTHOR SPECIFICALLY DISCLAIMS ANY IMPLIED
 * WARRANTIES OR CONDITIONS OF MERCHANTABILITY, SATISFACTORY QUALITY,
 * NON-INFRINGEMENT AND FITNESS FOR A PARTICULAR PURPOSE.
 */

using System;
using System.IO;
using System.Text;
using System.Xml;

namespace VMware.Binding.WsTrust
{
    /// <summary>
    ///    A utility class that creates a <see cref="XmlWriter" /> that writes to
    ///    a <see cref="XmlDocument" />.
    /// </summary>
    public class XmlDocumentWriterHelper : IDisposable
    {
        private readonly MemoryStream _bufferMemoryStream;
        private XmlWriter _writer;

        public XmlDocumentWriterHelper()
        {
            _bufferMemoryStream = new MemoryStream();
        }

        /// <summary>
        ///    Creates a <see cref="XmlDictionaryWriter" /> that would write its content to a <see cref="XmlDocument" />.
        ///    Only one writer can be created for this instance of the utility class.
        /// </summary>
        public XmlWriter CreateDocumentWriter()
        {
            if (_writer != null)
            {
                throw new InvalidOperationException("Cannot get a second document writer.");
            }

            // Current implementation uses a memory buffer. Ideally, the writer should
            // write directly to a XmlDocument.
            _writer = XmlDictionaryWriter.CreateTextWriter(_bufferMemoryStream, Encoding.UTF8, false);

            return _writer;
        }

        public XmlDocument ReadDocument()
        {
            if (_writer == null)
            {
                throw new InvalidOperationException("Cannot read the document ouput until the writer was used.");
            }

            _writer.Close();
            _bufferMemoryStream.Seek(0, SeekOrigin.Begin);

            return Util.ReadDocument(_bufferMemoryStream, true);
        }

        public void Dispose()
        {
            if (_writer != null)
            {
                _writer.Dispose();
            }

            _bufferMemoryStream.Dispose();
        }
    }
}