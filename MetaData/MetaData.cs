﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Policy;
using System.Text;
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

        private readonly Dictionary<string, XElement> entityTypes = new Dictionary<string, XElement>();
        private readonly Dictionary<string, XElement> entitySets = new Dictionary<string, XElement>();

        public event EventHandler ReadCompleted;

        public EntityContainer Model { get; }

        public MetaData(string ServiceUrl, string UserName, string UserPassword)
        {
            ServiceUri = new Uri(ServiceUrl);
            proxy = new WebClient();

            if (UserName != null && UserName != string.Empty)
            {
                proxy.UseDefaultCredentials = false;

                NetworkCredential networkCredential = new NetworkCredential(UserName, UserPassword);
                proxy.Credentials = networkCredential;
                
                string host = new Uri(ServiceUrl).GetLeftPart(UriPartial.Authority);

                CredentialCache myCredentialCache = new CredentialCache
                {
                    {
                        new Uri(host),
                        "Basic",
                        networkCredential
                    }
                };
                //proxy.PreAuthenticate = true;
                proxy.Credentials = myCredentialCache;
            }
            else
            {
                proxy.UseDefaultCredentials = true;
            }
            

            proxy.Headers.Add("Content-Type", "application/xml, application/atom+xml, application/json");
            XmlDoc = new XDocument();
            Model = new EntityContainer();

            proxy.OpenReadAsync(ServiceUri);
            proxy.OpenReadCompleted += new OpenReadCompletedEventHandler(Proxy_OpenReadCompleted);
            //LoadModel();
        }

        private void Proxy_OpenReadCompleted(object sender, OpenReadCompletedEventArgs e)
        {
            //try
            //{
            using (StreamReader sr = new StreamReader(e.Result))
            {
                XmlDoc = XDocument.Parse(sr.ReadToEnd());
            }
            BuildModel();
            //}
            //catch (Exception err)
            //{
            //    MessageBox.Show(err.Message);
            //}
        }

        private void BuildModel()
        {
            if (XmlDoc.Root == null)
            {
                MessageBox.Show("BAD DOCUMENT");
                return;
            }

            IEnumerable<XElement> entityContainer = XmlDoc.Root.Descendants().Where(d => d.Name.LocalName == "EntityContainer");
            //IEnumerable<XElement> entityContainer = XmlDoc.Root.Descendants(EDMNS + "EntityContainer");
            if (entityContainer != null && entityContainer.Count() > 0)
            {
                EDMNS = entityContainer.First().Name.Namespace;
                Model.Name = entityContainer.First().Attribute("Name").Value;
            }


            foreach (XElement schema in XmlDoc.Root.Descendants(EDMNS + "Schema"))
            {
                string ns = schema.Attribute("Namespace").Value;
                foreach (XElement et in schema.Descendants(EDMNS + "EntityType"))
                {
                    entityTypes.Add(ns + "." + et.Attribute("Name").Value, et);
                }

                foreach (XElement et in XmlDoc.Root.Descendants(EDMNS + "EntitySet"))
                {
                    string name = et.Attribute("Name").Value; //  ns + "." + et.Attribute("Name").Value;
                    if (entitySets.ContainsKey(name) == false)
                    {
                        entitySets.Add(name, et);
                    }
                }
            }

            BuildEntities();
            BuildEntitySets();
            BuildAssociationSet();
            BuildEntitySetsEntities();

            ReadCompleted?.Invoke(this, EventArgs.Empty);
        }

        private void BuildEntities()
        {
            foreach (KeyValuePair<string, XElement> kvp in entityTypes)
            {
                XElement item = kvp.Value;
                string name = item.Attribute("Name").Value;
                string entityType = kvp.Key; //item.Attribute("EntityType")?.Value;
                string baseType = item.Attribute("BaseType")?.Value;

                Entity entity = new Entity()
                {
                    Name = name,
                    NameType = entityType,
                    BaseType = baseType
                };
                Model.Entities.Add(entityType, entity);
            }
        }

        private void BuildEntitySets()
        {
            foreach (KeyValuePair<string, XElement> kvp in entitySets)
            {
                XElement item = kvp.Value;
                if (Model.Entities.TryGetValue(item.Attribute("EntityType").Value, out Entity entity))
                {
                    string name = item.Attribute("Name").Value;
                    if (Model.EntitySets.ContainsKey(name) == false)
                    {
                        Model.EntitySets.Add(
                            key: name,
                            value: new EntitySet(
                                name: name,
                                entity: entity));
                    }
                    
                }
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

        private void BuildEntitySetsEntities()
        {
            foreach (EntitySet entitySet in Model.EntitySets.Values)
            {
                XElement xe = entityTypes[entitySet.NameType];
                XElement se = entitySets[entitySet.Name];

                BuildEntityKeys(entitySet.Entity, xe);
                BuildEntityProperties(entitySet.Entity, xe);
                BuildEntityNavigationProperties(
                    entitySet: entitySet,
                    xSetElement: se,
                    xElement: xe);
            }
        }

        private void BuildEntityProperties(Entity entity, XElement xe)
        {
            foreach (XElement prop in xe.Elements(EDMNS + "Property"))
            {
                if (!entity.Properties.ContainsKey(prop.Attribute("Name").Value))
                {
                    EDMProperty p = new EDMProperty
                    {
                        Name = prop.Attribute("Name").Value,
                        NameType = prop.Attribute("Type").Value,
                        Nullable = bool.Parse(prop.Attribute("Nullable")?.Value ?? "true")
                    };
                    //p.MaxLength   = int.Parse(prop.Attribute("MaxLength").Value);
                    //p.FixedLength = bool.Parse(prop.Attribute("FixedLength").Value);
                    //p.Unicode     = bool.Parse(prop.Attribute("Unicode").Value);

                    entity.Properties.Add(p.Name, p);
                }
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
                if (!e.Keys.ContainsKey(key.Attribute("Name").Value))
                {
                    e.Keys.Add(key.Attribute("Name").Value, key.Attribute("Name").Value);
                }  
            }
        }

        private void BuildEntityNavigationProperties(EntitySet entitySet, XElement xSetElement, XElement xElement)
        {
            Entity entity = entitySet.Entity;

            string FromRole;
            string ToRole;
            string Relationship;
            string KeyNav;

            //string Target;
            //string Path;

            string elementName = xElement.Attribute("Name").Value;

            foreach (XElement navi in xElement.Elements(EDMNS + "NavigationProperty"))
            {
                KeyNav = navi.Attribute("Name").Value;
                FromRole = navi.Attribute("FromRole")?.Value;
                ToRole = navi.Attribute("ToRole")?.Value;
                Relationship = navi.Attribute("Relationship")?.Value; //.Split('.').Last();

                if (!string.IsNullOrEmpty(Relationship) && !string.IsNullOrEmpty(ToRole) && !string.IsNullOrEmpty(FromRole) && Model.AssociationSet.ContainsKey(Relationship))
                {
                    Association ass = Model.AssociationSet[Relationship];

                    string es = (from x in ass.EndRoles
                                 where x.Role == ToRole
                                 select x.EntitySet).First();

                    entity.NavigationProperties.Add(KeyNav, Model.EntitySets[es].Entity);
                }
                else
                {
                    if (!entity.NavigationProperties.ContainsKey(navi.Attribute("Name").Value))
                    {
                        entity.NavigationProperties.Add(navi.Attribute("Name").Value, null);
                    }
                    
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
