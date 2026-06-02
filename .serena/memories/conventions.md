# Code Conventions

- **Class naming**: PascalCase for public types, camelCase prefix for XML message types (e.g. `litleOnlineRequest`, `litleXmlSerializer`) — matches Vantiv XML element names
- **Interface naming**: `I`-prefix (e.g. `ILitleOnline`)
- **Configuration**: `Dictionary<string,string>` constructor overload OR `.dll.config` file
- **XML serialization**: System.Xml.Serialization attributes (`[XmlElement]`, `[XmlAttribute]`, `[XmlEnum]`)
- **Error handling**: `LitleOnlineException` for API-level errors
- **Event pattern**: `HttpActionEventArgs` for communication hooks
- **Test structure**: Unit/, Functional/, Certification/ directories under test project
- **Assembly output name**: Must remain `LitleSdkForNet` (drop-in replacement constraint)
