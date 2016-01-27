﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSON.Core.Parser;
using Microsoft.JSON.Core.Parser.TreeItems;
using Microsoft.JSON.Core.Schema;
using Microsoft.JSON.Core.Schema.Drafts.Draft4;

namespace StandaloneJsonValidator
{
    public static class JsonValidator
    {
        static JsonValidator()
        {
            ThreadPool.SetMaxThreads(10000, 10000);
        }

        public static async Task<bool> ValidateAsync(StringWithFileNameTextProvider instanceTextProvider, StringWithFileNameTextProvider schemaTextProvider)
        {
            return !(await ValidateAsync(instanceTextProvider, schemaTextProvider, Enumerable.Empty<IJSONSchemaFormatHandler>())).Errors.Any();
        }

        public static async Task<ValidationResult> ValidateAsync(StringWithFileNameTextProvider instanceTextProvider, StringWithFileNameTextProvider schemaTextProvider, IEnumerable<IJSONSchemaFormatHandler> formatHandlers)
        {
            JSONDocumentLoader loader = new JSONDocumentLoader();
            JSONParser parser = new JSONParser();
            JSONDocument schemaDoc = parser.Parse(schemaTextProvider);
            JSONDocument instanceDoc = parser.Parse(instanceTextProvider);
            List<JSONError> allErrors = new List<JSONError>();

            foreach (Tuple<JSONParseItem, JSONParseError> error in instanceDoc.GetContainedParseErrors())
            {
                allErrors.Add(new JSONError
                {
                    Kind = JSONErrorKind.Syntax,
                    Length = error.Item1.Length,
                    Start = error.Item1.Start,
                    Location = JSONErrorLocation.InstanceDocument,
                    Message = error.Item2.Text ?? error.Item2.ErrorType.ToString()
                });
            }

            foreach (Tuple<JSONParseItem, JSONParseError> error in schemaDoc.GetContainedParseErrors())
            {
                allErrors.Add(new JSONError
                {
                    Kind = JSONErrorKind.Syntax,
                    Length = error.Item1.Length,
                    Start = error.Item1.Start,
                    Location = JSONErrorLocation.Schema,
                    Message = error.Item2.Text ?? error.Item2.ErrorType.ToString()
                });
            }

            loader.SetCacheItem(new JSONDocumentLoadResult(schemaDoc));
            loader.SetCacheItem(new JSONDocumentLoadResult(instanceDoc));

            JSONSchemaDraft4EvaluationTreeNode tree = await JSONSchemaDraft4EvaluationTreeProducer.CreateEvaluationTreeAsync(instanceDoc, (JSONObject) schemaDoc.TopLevelValue, loader, formatHandlers).ConfigureAwait(false);

            foreach (JSONSchemaValidationIssue issue in tree.ValidationIssues)
            {
                allErrors.Add(new JSONError
                {
                    Kind = JSONErrorKind.Validation,
                    Start = issue.TargetItem.Start,
                    Length = issue.TargetItem.Length,
                    Location = JSONErrorLocation.InstanceDocument,
                    Message = issue.Message
                });
            }

            foreach (JSONSchemaSanityIssue issue in tree.SanityIssues)
            {
                allErrors.Add(new JSONError
                {
                    Kind = JSONErrorKind.Validation,
                    Start = issue.ParseItem.Start,
                    Length = issue.ParseItem.Length,
                    Location = JSONErrorLocation.Schema,
                    Message = issue.Message
                });
            }

            return new ValidationResult
            {
                InstanceDocumentText = instanceTextProvider.Text,
                SchemaText = schemaTextProvider.Text,
                Errors = allErrors
            };
        }

        public static Task<ValidationResult> ValidateAsync(string instanceText, string schemaText, IEnumerable<IJSONSchemaFormatHandler> formatHandlers)
        {
            StringWithFileNameTextProvider instanceProvider = new StringWithFileNameTextProvider(@"c:\temp\instance.json", instanceText);
            StringWithFileNameTextProvider schemaProvider = new StringWithFileNameTextProvider(@"c:\temp\schema.json", schemaText);
            return ValidateAsync(instanceProvider, schemaProvider, formatHandlers);
        }

        public static string Download(Uri location)
        {
            try
            {
                HttpWebRequest request = WebRequest.CreateHttp(location);
                using (WebResponse response = request.GetResponse())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8, true, 8192, true))
                {
                    return reader.ReadToEnd();
                }
            }
            catch (WebException)
            {
                return string.Empty;
            }
        }

        public static Task<ValidationResult> ValidateAsync(string instanceText, Uri schemaLocation, IEnumerable<IJSONSchemaFormatHandler> formatHandlers)
        {
            StringWithFileNameTextProvider instanceProvider = new StringWithFileNameTextProvider(@"c:\temp\instance.json", instanceText);

            StringWithFileNameTextProvider schemaProvider = null;

            if (schemaLocation != null)
            {
                string schemaText = Download(schemaLocation);
                schemaProvider = new StringWithFileNameTextProvider(schemaLocation.ToString(), schemaText);
            }

            return ValidateAsync(instanceProvider, schemaProvider, formatHandlers);
        }

        public static Task<ValidationResult> ValidateAsync(Uri instanceLocation, string schemaText, IEnumerable<IJSONSchemaFormatHandler> formatHandlers)
        {
            string instanceText = Download(instanceLocation);
            StringWithFileNameTextProvider instanceProvider = new StringWithFileNameTextProvider(instanceLocation.ToString(), instanceText);


            StringWithFileNameTextProvider schemaProvider = null;

            if (schemaText != null)
            {
                schemaProvider = new StringWithFileNameTextProvider(@"c:\temp\schema.json", schemaText);
            }

            return ValidateAsync(instanceProvider, schemaProvider, formatHandlers);
        }

        public static Task<ValidationResult> ValidateAsync(Uri instanceLocation, Uri schemaLocation, IEnumerable<IJSONSchemaFormatHandler> formatHandlers)
        {
            string instanceText = Download(instanceLocation);
            StringWithFileNameTextProvider instanceProvider = new StringWithFileNameTextProvider(instanceLocation.ToString(), instanceText);
            StringWithFileNameTextProvider schemaProvider = null;

            if (schemaLocation != null)
            {
                string schemaText = Download(schemaLocation);
                schemaProvider = new StringWithFileNameTextProvider(schemaLocation.ToString(), schemaText);
            }

            return ValidateAsync(instanceProvider, schemaProvider, formatHandlers);
        }
    }
}
