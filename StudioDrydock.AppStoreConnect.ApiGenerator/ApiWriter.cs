using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

namespace StudioDrydock.AppStoreConnect.ApiGenerator
{

    class ApiWriter : IDisposable
    {
        public readonly CsWriter cs;

        Dictionary<string, OpenApiSchema> schemas = new Dictionary<string, OpenApiSchema>();
        Dictionary<string, OpenApiSchema> enums = new Dictionary<string, OpenApiSchema>();
        HashSet<string> methodNames = new HashSet<string>();

        public ApiWriter(TextWriter writer)
        {
            cs = new CsWriter(writer);
            cs.WriteLine("#nullable enable");
            cs.WriteLine();
            cs.WriteLine("using System.Runtime.Serialization;");
            cs.WriteLine("using System.Text;");
            cs.WriteLine("using System.Text.Json;");
            cs.WriteLine("using System.Text.Json.Serialization;");
            cs.WriteLine("using Macross.Json.Extensions;");
            cs.WriteLine();
            cs.BeginBlock("namespace StudioDrydock.AppStoreConnect.Api");
        }

        public void Dispose()
        {
            cs.EndBlock(); // namespace
        }

        public void GenerateClass(string name, OpenApiSchema schema)
        {
            var isNextLink = name == "Links" && schema.Properties.ContainsKey("next");
            var hasNextLink = schema.Properties.ContainsKey("links") && schema.Properties["links"].Properties.ContainsKey("next");
            var isUploadOperations = name == "UploadOperations";
            var isRequestHeaders = name == "RequestHeaders";

            cs.WriteLine($"public class {name}");

            if (hasNextLink)
            {
                cs.WriteLine("    : IHasNextLink");
            }
            else if (isNextLink)
            {
                cs.WriteLine("    : INextLink");
            }
            else if (isUploadOperations)
            {
                cs.WriteLine("    : IUploadOperations");
            }
            else if (isRequestHeaders)
            {
                cs.WriteLine("    : IRequestHeaders");
            }

            cs.BeginBlock();

            // Generate inner classes required by properties
            foreach (var kv in schema.Properties)
                GenerateAnonymousPropertyTypes(kv.Key, kv.Value);

            // Properties
            var requiredProperties = new HashSet<string>(schema.Required);
            foreach (var kv in schema.Properties)
                GenerateProperty(kv.Key, kv.Value, required: requiredProperties.Contains(kv.Key));

            if (hasNextLink)
            {
                cs.WriteLine("INextLink IHasNextLink.links => links;");
            }
            if (isUploadOperations)
            {
                cs.WriteLine("IReadOnlyList<IRequestHeaders>? IUploadOperations.requestHeaders => requestHeaders;");
            }

            cs.EndBlock();
        }

        public void GenerateProperty(string name, OpenApiSchema schema, bool required)
        {
            GenerateEnum(name, schema, trailingNewLine: false);

            cs.BeginLine();
            cs.Write("public ");
            WriteType(name, schema);
            if (!required)
                cs.Write("?");
            cs.Write($" {name} {{ get; set; }}");
            if (required)
            {
                cs.Write(" = ");
                WriteDefaultValue(name, schema);
                cs.Write(";");
            }
            cs.EndLine();
        }

        bool GenerateEnum(string nameHint, OpenApiSchema schema, bool trailingNewLine = true)
        {
            if (schema.Type == "array")
                return GenerateEnum(nameHint, schema.Items);

            if (schema.Type == "string" && schema.Enum != null && schema.Enum.Count > 1)
            {
                cs.WriteLine("[JsonConverter(typeof(JsonStringEnumMemberConverter))]");
                cs.BeginBlock($"public enum {nameHint.TitleCase()}");
                foreach (var value in schema.Enum)
                {
                    string stringValue = ((OpenApiString)value).Value;
                    string identifierValue = stringValue.MakeValidEnumIdentifier();
                    if (stringValue != identifierValue)
                        cs.WriteLine($"[EnumMember(Value = \"{stringValue}\")]");
                    cs.WriteLine($"{identifierValue},");
                }
                cs.EndBlock(trailingNewLine: trailingNewLine);
                return true;
            }

            return false;
        }

        bool IsEnumCompatible(OpenApiSchema a, OpenApiSchema b)
        {
            if (a.Enum.Count != b.Enum.Count)
                return false;

            var valuesA = a.Enum
                .Cast<OpenApiString>()
                .Select(x => x.Value)
                .OrderBy(x => x)
                .ToArray();
            var valuesB = b.Enum
                .Cast<OpenApiString>()
                .Select(x => x.Value)
                .OrderBy(x => x)
                .ToArray();
            for (int i = 0; i < valuesA.Length; ++i)
            {
                if (valuesA[i] != valuesB[i])
                    return false;
            }
            return true;
        }

        public void WriteType(string nameHint, OpenApiSchema schema)
        {
            if (schema.Type == null && schema.OneOf.Count >= 1)
            {
                var first = schema.OneOf.First();
                if (schema.OneOf.All(x => x.Type == first.Type && x.Title == first.Title))
                {
                    // Schema includes a OneOf with multiple options that are
                    // all equivalent; just pick the first.
                    schema = first;
                }
                else
                {
                    // Union schemas not supported
                    cs.Write("object");
                    return;
                }
            }

            switch (schema.Type)
            {
                case "array":
                    WriteType(nameHint, schema.Items);
                    cs.Write("[]");
                    break;
                case "string":
                    if (schema.Enum != null && schema.Enum.Count > 1)
                        cs.Write($"{nameHint.TitleCase()}");
                    else
                        cs.Write("string");
                    break;
                case "boolean":
                    cs.Write("bool");
                    break;
                case "integer":
                    cs.Write("int");
                    break;
                case "number":
                    cs.Write("double");
                    break;
                case "object":
                    // Reference anonymous property type generated earlier
                    cs.Write(nameHint.TitleCase());
                    break;
                default:
                    throw new NotSupportedException($"Schema type {schema.Type} not supported");
            }
        }

        void WriteDefaultValue(string nameHint, OpenApiSchema schema)
        {
            if (schema.Type == null && schema.OneOf.Count >= 1)
            {
                var first = schema.OneOf.First();
                if (schema.OneOf.All(x => x.Type == first.Type && x.Title == first.Title))
                {
                    // Schema includes a OneOf with multiple options that are
                    // all equivalent; just pick the first.
                    schema = first;
                }
                else
                {
                    // Union schemas not supported
                    cs.Write("null");
                    return;
                }
            }

            switch (schema.Type)
            {
                case "array":
                    cs.Write("{ }");
                    break;
                case "string":
                    if (schema.Enum?.Count == 1)
                        cs.Write($"\"{((OpenApiString)schema.Enum[0]).Value}\"");
                    else if (schema.Enum != null && schema.Enum.Count > 1)
                        cs.Write("default");
                    else
                        cs.Write("\"\"");
                    break;
                case "boolean":
                    cs.Write("false");
                    break;
                case "integer":
                    cs.Write("0");
                    break;
                case "number":
                    cs.Write("0.0");
                    break;
                case "object":
                    // Reference anonymous property type generated earlier
                    cs.Write($"new {nameHint.TitleCase()}()");
                    break;
                default:
                    throw new NotSupportedException($"Schema type {schema.Type} not supported");
            }
        }

        void GenerateAnonymousPropertyTypes(string nameHint, OpenApiSchema schema)
        {
            if (schema.Type == null && schema.OneOf.Count >= 1)
            {
                var first = schema.OneOf.First();
                if (schema.OneOf.All(x => x.Type == first.Type && x.Title == first.Title))
                {
                    // Schema includes a OneOf with multiple options that are
                    // all equivalent; just pick the first.
                    GenerateAnonymousPropertyTypes(nameHint, first);
                }
            }

            switch (schema.Type)
            {
                case "array":
                    GenerateAnonymousPropertyTypes(nameHint, schema.Items);
                    break;
                case "object":
                    GenerateClass(nameHint.TitleCase(), schema);
                    break;
            }
        }

        bool IsReferenceType(OpenApiSchema schema)
        {
            if (schema.Type == null && schema.OneOf.Count >= 1)
            {
                var first = schema.OneOf.First();
                if (schema.OneOf.All(x => x.Type == first.Type && x.Title == first.Title))
                {
                    // Schema includes a OneOf with multiple options that are
                    // all equivalent; just pick the first.
                    schema = first;
                }
                else
                {
                    // Union schemas not supported
                    cs.Write("null");
                    return true;
                }
            }

            switch (schema.Type)
            {
                case "array":
                case "string":
                case "object":
                    return true;
                case "boolean":
                case "integer":
                case "number":
                    return false;
                default:
                    throw new NotSupportedException($"Schema type {schema.Type} not supported");
            }
        }

        bool IsSuccessStatusCode(int statusCode) => statusCode >= 200 && statusCode < 300;

        string FormatRequestMethodName(OperationType operationType, string operationId)
        {
            string method = operationType.ToString();
            if (operationId.Contains('-'))
                operationId = operationId.Substring(0, operationId.LastIndexOf('-'));
            operationId = operationId.MakeValidIdentifier().TitleCase();
            return $"{method}{operationId}";
        }

        void GenerateTopLevelEnum(string name, OpenApiSchema schema)
        {
            if (enums.TryGetValue(name, out var existing))
            {
                if (!IsEnumCompatible(existing, schema))
                    throw new NotSupportedException($"Multiple top-level enums with same name {name}");
                return;
            }

            if (GenerateEnum(name, schema))
                enums[name] = schema;
        }

        void GenerateTopLevelClass(string name, OpenApiSchema schema)
        {
            if (schemas.TryGetValue(name, out var existing))
            {
                if (existing != schema)
                    throw new NotSupportedException($"Multiple top-level schemas with same name {name}");
                return;
            }

            schemas[name] = schema;
            GenerateClass(name, schema);
        }

        public void GenerateOperation(string path, OpenApiPathItem pathItem, OperationType operationType, OpenApiOperation operation)
        {
            var response = operation.Responses.FirstOrDefault(x => IsSuccessStatusCode(int.Parse(x.Key))).Value;
            if (response == null)
                throw new NotSupportedException($"No response with success status code for {operation.OperationId}");

            string methodName = FormatRequestMethodName(operationType, operation.OperationId);
            string methodNameSuffix = "";

            // check if we already have a method with the same name, if we do, append the version number as a suffix:
            if (!methodNames.Add($"{operationType}{operation.OperationId}"))
            {
                var splits = path.Split("/");
                if (splits.Length < 2)
                    throw new NotSupportedException($"Path {path} is not supported");
                methodNameSuffix = splits[1].TitleCase();
            }

            // Request type
            OpenApiSchema? requestSchema = null;
            string requestSchemaName = $"{methodName}Request{methodNameSuffix}";
            if (operation.RequestBody != null && operation.RequestBody.Content.TryGetValue("application/json", out var requestBodyContent))
            {
                requestSchema = requestBodyContent.Schema;
                if (!string.IsNullOrEmpty(requestSchema.Title))
                    requestSchemaName = requestSchema.Title;
                GenerateTopLevelClass(requestSchemaName, requestSchema);
            }

            // Response type
            OpenApiSchema? responseSchema = null;
            string responseSchemaName = $"{methodName}Response{methodNameSuffix}";
            if (response.Content.TryGetValue("application/json", out var responseContent))
            {
                responseSchema = responseContent.Schema;
                if (!string.IsNullOrEmpty(responseSchema.Title))
                    responseSchemaName = responseSchema.Title;
                GenerateTopLevelClass(responseSchemaName, responseSchema);
            }

            // Query parameters
            var queryParameters = operation.Parameters
                .Where(x => !x.Deprecated)
                .DistinctBy(x => x.Name)        // Duplicate fields[inAppPurchases] parameter in /v1/ciProducts/{id}/app
                .OrderBy(x => !x.Required);

            // Enums required for query parameters
            foreach (var param in queryParameters)
                GenerateTopLevelEnum($"{methodName}{param.Name.MakeValidIdentifier().TitleCase()}{methodNameSuffix}", param.Schema);

            cs.Comment(path);
            if (operation.Deprecated)
            {
                cs.BeginLine();
                cs.Write("[Obsolete]");
                cs.EndLine();
            }
            cs.BeginLine();
            cs.Write("public ");
            if (responseSchema != null)
            {
                cs.Write("Task<");
                WriteType(responseSchemaName, responseSchema);
                cs.Write(">");
            }
            else
            {
                cs.Write("Task");
            }
            cs.Write($" {methodName}{methodNameSuffix}(");
            cs.BeginCommaDelimitedList();

            // Path parameters
            foreach (var param in pathItem.Parameters)
            {
                cs.WriteCommaIfRequired();
                WriteType("???", param.Schema);
                cs.Write($" {param.Name.MakeValidIdentifier()}");
            }

            // Request body
            if (requestSchema != null)
            {
                cs.WriteCommaIfRequired();
                WriteType(requestSchemaName, requestSchema);
                cs.Write(" request");
            }

            // Query parameters
            foreach (var param in queryParameters)
            {
                cs.WriteCommaIfRequired();
                WriteType($"{methodName}{param.Name.MakeValidIdentifier().TitleCase()}{methodNameSuffix}", param.Schema);
                if (!param.Required)
                    cs.Write("?");
                cs.Write($" {param.Name.MakeValidIdentifier()}");
                if (!param.Required)
                    cs.Write(" = default");
            }
            cs.Write(")");
            cs.EndLine();

            cs.BeginBlock();
            cs.WriteLine($"string path = \"{path}\";");
            foreach (var param in pathItem.Parameters)
                cs.WriteLine($"path = path.Replace(\"{{{param.Name}}}\", {param.Name.MakeValidIdentifier()}.ToString());");
            cs.WriteLine($"var uriBuilder = new UriBuilder(baseUri, path);");
            foreach (var param in queryParameters)
            {
                if (IsReferenceType(param.Schema))
                {
                    cs.WriteLine($"if ({param.Name.MakeValidIdentifier()} != null)");
                    cs.BeginLine();
                    cs.Write("    ");
                }
                else if (!param.Required)
                {
                    cs.WriteLine($"if ({param.Name.MakeValidIdentifier()}.HasValue)");
                    cs.BeginLine();
                    cs.Write("    ");
                }
                cs.Write($"uriBuilder.AddParameter(\"{param.Name}\", ");
                WriteParameterToString(param);
                cs.Write(");");
                cs.EndLine();
            }

            cs.WriteLine();
            cs.WriteLine($"var message = new HttpRequestMessage(HttpMethod.{operationType}, uriBuilder.uri);");
            if (requestSchema != null)
                cs.WriteLine("message.Content = Serialize(request);");

            if (responseSchema != null)
                cs.WriteLine($"return SendAsync<{responseSchemaName}>(message);");
            else
                cs.WriteLine("return SendAsync(message);");
            cs.EndBlock();
        }

        void WriteParameterToString(OpenApiParameter param)
        {
            var schema = param.Schema;
            if (schema.Type == null && schema.OneOf.Count >= 1)
            {
                var first = schema.OneOf.First();
                if (schema.OneOf.All(x => x.Type == first.Type && x.Title == first.Title))
                {
                    // Schema includes a OneOf with multiple options that are
                    // all equivalent; just pick the first.
                    schema = first;
                }
                else
                {
                    // Union schemas not supported
                    cs.Write("object");
                    return;
                }
            }

            switch (schema.Type)
            {
                case "array":
                    cs.Write("string.Join(\",\", ");
                    cs.Write(param.Name.MakeValidIdentifier());
                    cs.Write(")");
                    break;
                case "string":
                    cs.Write(param.Name.MakeValidIdentifier());
                    break;
                case "object":
                    throw new NotSupportedException("Parameter cannot be object type");
                case "boolean":
                case "integer":
                case "number":
                    cs.Write(param.Name.MakeValidIdentifier());
                    if (!param.Required)
                        cs.Write(".Value");
                    cs.Write(".ToString()");
                    break;
                default:
                    throw new NotSupportedException($"Schema type {schema.Type} not supported");
            }
        }
    }

}