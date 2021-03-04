using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace Extended.Serialization
{
    /// <summary> Serializes and deserializes instances of type <typeparamref name="T"/> into and from XML documents. </summary>
    /// <typeparam name="T"> The type of object that is serialized or deserialized. </typeparam>
    public class XmlSerializer<T> : XmlSerializer
    {
        #region Constructors

        /// <summary> Initializes a new instance that can serialize objects of type <typeparamref name="T"/> into XML documents, and deserialize XML documents into instances of type <typeparamref name="T"/>. </summary>
        public XmlSerializer() : base(typeof(T))
        {
        }

        /// <summary> Initializes a new instance using an object that maps one type to another. </summary>
        public XmlSerializer(XmlTypeMapping xmlTypeMapping)
            : base(xmlTypeMapping)
        {
        }

        /// <summary> Initializes a new instance that can serialize objects of type <typeparamref name="T"/> into XML documents, and deserialize XML documents into instances of type <typeparamref name="T"/>. Specifies the default namespace for all the XML elements. </summary>
        public XmlSerializer(string defaultNamespace)
            : base(typeof(T), defaultNamespace)
        {
        }

        /// <summary> Initializes a new instance that can serialize objects of type <typeparamref name="T"/> into XML documents, and deserialize XML documents into instances of type <typeparamref name="T"/>. If a property or field returns an array, the <paramref name="extraTypes"/> parameter specifies objects that can be inserted into the array. </summary>
        public XmlSerializer(Type[] extraTypes)
            : base(typeof(T), extraTypes)
        {
        }

        /// <summary> Initializes a new instance that can serialize objects of type <typeparamref name="T"/> into XML documents, and deserialize XML documents into instances of type <typeparamref name="T"/>. Each object to be serialized can itself contain instances of classes, which this overload can override with other classes. </summary>
        public XmlSerializer(XmlAttributeOverrides overrides)
            : base(typeof(T), overrides)
        {
        }

        /// <summary> Initializes a new instance that can serialize objects of type <typeparamref name="T"/> into XML documents, and deserialize an XML document into instances of type <typeparamref name="T"/>. It also specifies the class to use as the XML root element. </summary>
        public XmlSerializer(XmlRootAttribute root)
            : base(typeof(T), root)
        {
        }

        /// <summary> Initializes a new instance that can serialize objects of type <typeparamref name="T"/> into XML document instances, and deserialize XML document instances into instances of type <typeparamref name="T"/>. Each instance to be serialized can itself contain instances of classes, which this overload overrides with other classes. This overload also specifies the default namespace for all the XML elements and the class to use as the XML root element. </summary>
        public XmlSerializer(XmlAttributeOverrides overrides, Type[] extraTypes, XmlRootAttribute root, string defaultNamespace)
            : base(typeof(T), overrides, extraTypes, root, defaultNamespace)
        {
        }

        /// <summary> Initializes a new instance that can serialize objects of type <typeparamref name="T"/> into XML document instances, and deserialize XML document instances into instances of type <typeparamref name="T"/>. Each instance to be serialized can itself contain instances of classes, which this overload overrides with other classes. This overload also specifies the default namespace for all the XML elements and the class to use as the XML root element, and the location of the types. </summary>
        public XmlSerializer(XmlAttributeOverrides overrides, Type[] extraTypes, XmlRootAttribute root, string defaultNamespace, string location)
            : base(typeof(T), overrides, extraTypes, root, defaultNamespace, location)
        {
        }

        #endregion

        #region Properties

        /// <summary> A singleton instance that is stored in memory and can be reused. </summary>
        public static XmlSerializer<T> Default { get; } = new XmlSerializer<T>();

        #endregion

        #region Methods

        /// <summary> Serializes the specified instance of type <typeparamref name="T"/> to XML using default settings.
        /// <br/> XML declaration and namespaces will be omitted. Elements will be indented. </summary>
        public string Serialize(T value)
        {
            var xmlSettings = new XmlWriterSettings();
            xmlSettings.OmitXmlDeclaration = true;
            xmlSettings.Indent = true;

            var xmlNamespaces = new XmlSerializerNamespaces();
            xmlNamespaces.Add(prefix: string.Empty, ns: string.Empty);

            return this.Serialize(value, xmlSettings, xmlNamespaces);
        }

        /// <summary> Serializes the specified instance of type <typeparamref name="T"/> to XML using specified settings. </summary>
        public string Serialize(T value, XmlWriterSettings xmlSettings, XmlSerializerNamespaces xmlNamespaces)
        {
            using var stringWriter = new StringWriter();
            using var xmlWriter = XmlWriter.Create(stringWriter, xmlSettings);
            this.Serialize(xmlWriter, value, xmlNamespaces);
            xmlWriter.Flush();
            return stringWriter.ToString();
        }

        /// <summary> Deserializes the specified <see cref="string"/> to an instance of type <typeparamref name="T"/>. </summary>
        public T Deserialize(string value)
        {
            using var xmlNodeReader = new StringReader(value);
            return (T) this.Deserialize(xmlNodeReader);
        }

        #endregion
    }
}