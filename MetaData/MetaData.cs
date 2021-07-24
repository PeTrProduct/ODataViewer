using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows;
using System.Xml.Linq;

namespace ODataViewer
{
    public class MetaData
    {
        // Old .NET 3.0
        // const string EDMNS = "{http://schemas.microsoft.com/ado/2006/04/edm}";

        // .NET 3.5 & 4.0
        private static XNamespace EDMNS = "";
        private readonly Uri ServiceUri;
        private readonly WebClient proxy;
        private XDocument XmlDoc;

        private readonly Dictionary<string, XElement> EntityTypes = new Dictionary<string, XElement>();
        private readonly Dictionary<string, XElement> EntitySets = new Dictionary<string, XElement>();

        public event EventHandler ReadCompleted;

        public EntityContainer Model { get; }

        public MetaData(string ServiceUrl)
        {
            ServiceUri = new Uri(ServiceUrl);
            proxy = new WebClient
            {
                UseDefaultCredentials = true
            };
            proxy.Headers.Add("Content-Type", "application/xml, application/atom+xml, application/json");
            XmlDoc = new XDocument();
            Model = new EntityContainer();

            proxy.OpenReadAsync(ServiceUri);
            proxy.OpenReadCompleted += new OpenReadCompletedEventHandler(proxy_OpenReadCompleted);
            //LoadModel();
        }

        private void proxy_OpenReadCompleted(object sender, OpenReadCompletedEventArgs e)
        {
            try
            {
                using (StreamReader sr = new StreamReader(e.Result))
                {
                    XmlDoc = XDocument.Parse(sr.ReadToEnd());
                }
                BuildModel();
            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message);
            }
        }

        private void BuildModel()
        {
            if (XmlDoc.Root != null)
            {
                IEnumerable<XElement> entityContainer = XmlDoc.Root.Descendants().Where(d => d.Name.LocalName == "EntityContainer");
                //IEnumerable<XElement> entityContainer = XmlDoc.Root.Descendants(EDMNS + "EntityContainer");
                if (entityContainer != null && entityContainer.Count() > 0)
                {
                    EDMNS = entityContainer.First().Name.Namespace;
                    Model.Name = entityContainer.First().Attribute("Name").Value;
                }
            }

            BuildEntitySets();
            BuildAssociationSet();
            BuildEntities();

            if (ReadCompleted != null)
            {
                ReadCompleted(this, EventArgs.Empty);
            }
        }

        private void BuildEntitySets()
        {
            foreach (XElement item in XmlDoc.Root.Descendants(EDMNS + "EntitySet"))
            {
                Model.EntitySets.Add(
                    item.Attribute("Name").Value,
                    new EntitySet(
                        name: item.Attribute("Name").Value,
                        entityType: item.Attribute("EntityType").Value));
            }
        }

        private void BuildAssociationSet()
        {
            foreach (XElement schema in XmlDoc.Root.Descendants(EDMNS + "Schema"))
            {
                string ns = schema.Attribute("Namespace").Value;

                foreach (XElement item in schema.Descendants(EDMNS + "AssociationSet"))
                {
                    string key = item.Attribute("Association")?.Value ?? ns + "." + item.Attribute("Name").Value;
                    Association ass = new Association();
                    Model.AssociationSet.Add(key, ass);

                    foreach (XElement er in item.Elements())
                    {
                        ass.EndRoles.Add(
                            new EndRole
                            {
                                Role = er.Attribute("Role").Value,
                                EntitySet = er.Attribute("EntitySet").Value
                            }
                        );
                    }
                }
            }
        }

        private void BuildEntities()
        {
            

            IEnumerable<XElement> items = XmlDoc.Root.Descendants(EDMNS + "EntitySet");
            //

            foreach (XElement schema in XmlDoc.Root.Descendants(EDMNS + "Schema"))
            {
                string ns = schema.Attribute("Namespace").Value;
                foreach (XElement et in schema.Descendants(EDMNS + "EntityType"))
                {
                    EntityTypes.Add(ns + "." + et.Attribute("Name").Value, et);
                }

                foreach (XElement et in XmlDoc.Root.Descendants(EDMNS + "EntitySet"))
                {
                    if (EntitySets.ContainsKey(et.Attribute("Name").Value) == false)
                    {
                        EntitySets.Add(et.Attribute("Name").Value, et);
                    }
                }
            }

            foreach (EntitySet item in Model.EntitySets.Values)
            {
                XElement xe = EntityTypes[item.NameType];
                XElement se = EntitySets[item.Name];

                BuildEntityKeys(item.Entity, xe);
                BuildEntityProperties(item.Entity, xe);
                BuildEntityNavigationProperties(
                    entitySet: item,
                    xSetElement: se,
                    xElement: xe);
            }
        }

        private void BuildEntityProperties(Entity e, XElement xe)
        {
            foreach (XElement prop in xe.Elements(EDMNS + "Property"))
            {
                Property p = new Property
                {
                    Name = prop.Attribute("Name").Value,
                    NameType = prop.Attribute("Type").Value,
                    Nullable = bool.Parse(prop.Attribute("Nullable")?.Value ?? "true")
                };
                //p.MaxLength   = int.Parse(prop.Attribute("MaxLength").Value);
                //p.FixedLength = bool.Parse(prop.Attribute("FixedLength").Value);
                //p.Unicode     = bool.Parse(prop.Attribute("Unicode").Value);


                e.Properties.Add(p.Name, p);
            }
        }

        private void BuildEntityKeys(Entity e, XElement xe)
        {
            XElement keys = xe.Element(EDMNS + "Key");

            if (keys == null)
            {
                return;
            }

            foreach (XElement key in keys.Elements(EDMNS + "PropertyRef"))
            {
                e.Keys.Add(key.Attribute("Name").Value, key.Attribute("Name").Value);
            }
        }

        private void BuildEntityNavigationProperties(EntitySet entitySet, XElement xSetElement, XElement xElement)
        {
            Entity entity = entitySet.Entity;

            string FromRole;
            string ToRole;
            string Relationship;
            string KeyNav;

            string Target;
            string Path;

            string elementName = xElement.Attribute("Name").Value;

            foreach (XElement navi in xElement.Elements(EDMNS + "NavigationProperty"))
            {
                KeyNav = navi.Attribute("Name").Value;
                FromRole = navi.Attribute("FromRole")?.Value;
                ToRole = navi.Attribute("ToRole")?.Value;
                Relationship = navi.Attribute("Relationship")?.Value; //.Split('.').Last();

                if (!string.IsNullOrEmpty(Relationship) && !string.IsNullOrEmpty(ToRole) && !string.IsNullOrEmpty(FromRole))
                {
                    Association ass = Model.AssociationSet[Relationship];

                    string es = (from x in ass.EndRoles
                                 where x.Role == ToRole
                                 select x.EntitySet).First();

                    entity.NavigationProperties.Add(KeyNav, Model.EntitySets[es].Entity);
                }
                else
                {
                    entity.NavigationProperties.Add(
                        navi.Attribute("Name").Value, null);
                }

                //if ( model.EntitySets.ContainsKey( ToRole ) )
                //    entity.NavigationProperties.Add( 
                //        navi.Attribute("Name").Value , model.EntitySets[ToRole].Entity );
                //else
                //{
                //    entity.NavigationProperties.Add(
                //        navi.Attribute("Name").Value, null );
                //}
            }

            //foreach (XElement navi in xSetElement.Elements(EDMNS + "NavigationPropertyBinding"))
            //{
            //    Target = navi.Attribute("Target").Value;
            //    Path = navi.Attribute("Path").Value;

            //    if (entity.NavigationProperties.ContainsKey(Target) == false 
            //        && Model.EntitySets.ContainsKey(Target))
            //    {
            //        entity.NavigationProperties.Add(Target, Model.EntitySets[Target].Entity);
            //    }
            //}
        }
    }
}
