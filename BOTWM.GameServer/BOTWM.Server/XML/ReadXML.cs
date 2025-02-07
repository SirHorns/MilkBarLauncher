using System.Xml;

namespace BOTWM.Server.XML
{
    public static class ReadXml
    {
        public static Dictionary<string, Dictionary<string, string>> ReadAnimationFile()
        {
            var result = new Dictionary<string, Dictionary<string, string>>();
            var reader = new XmlTextReader(new StringReader(Resources.animationHashes));
            var animation = new Dictionary<string, string>();
            var animationCopy = new Dictionary<string, string>();
            string[] acceptedStrings = { "Hash", "Schedule", "Animation", "Name" };
            var next = "";
            var hash = "";

            animation.Add("Schedule", "");
            animation.Add("Animation", "");
            animation.Add("Name", "");

            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        if (acceptedStrings.Contains(reader.Name))
                        {
                            next = reader.Name;
                        }

                        break;

                    case XmlNodeType.Text:
                        if (next != "")
                        {
                            if (next == "Hash")
                            {
                                hash = reader.Value;
                            }
                            else
                            {
                                animation[next] = reader.Value;
                                if (next == "Animation")
                                {
                                    animationCopy = new Dictionary<string, string>(animation);
                                    result.Add(hash, animationCopy);
                                    next = "";
                                }
                            }
                        }

                        break;
                }
            }

            reader.Close();

            return result;
        }
    }
}