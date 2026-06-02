using System;
using System.Text;
using System.IO;
using System.Xml.Serialization;
using System.Xml;

namespace Litle.Sdk
{
    public class litleXmlSerializer
    {
        public virtual String SerializeObject(litleOnlineRequest req)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(litleOnlineRequest));
            using (MemoryStream ms = new MemoryStream())
            {
                try
                {
                    serializer.Serialize(ms, req);
                }
                catch (InvalidOperationException e)
                {
                    throw new LitleOnlineException("Failed to serialize request object", e);
                }
                return Encoding.UTF8.GetString(ms.ToArray()).Replace("\r\n", "\n");
            }
        }
    }
}
