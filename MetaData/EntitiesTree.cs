using System.Windows.Forms;

namespace ODataViewer
{
    public enum IconsEnum
    {
        Property,
        Nav,
        OpenFolder,
        CloseFolser,
        Keys,
        Key
    }
    public static class EntitiesTree
    {
        public static void BuildTree(this TreeView source, MetaData metaData)
        {
            source.BeginUpdate();
            TreeNode root = source.Nodes.Add(metaData.Model.Name);

            TreeNode entitySet = root.Nodes.Add("EntitySet");
            entitySet.ImageIndex = (int)IconsEnum.CloseFolser;

            foreach (EntitySet item in metaData.Model.EntitySets.Values)
            {
                // entitySet.Nodes.Add(item.NameType).BuildEntity(item.Entity);
                entitySet.Nodes.Add(item.Name).BuildEntity(item.Entity);
            }
            source.EndUpdate();
        }

        public static void BuildEntity(this TreeNode source, Entity entity)
        {
            TreeNode Keys = source.Nodes.Add("Keys");
            Keys.ImageIndex = (int)IconsEnum.Keys;

            foreach (System.Collections.Generic.KeyValuePair<string, string> k in entity.Keys)
            {
                Keys.Nodes.Add(k.Key).ImageIndex = (int)IconsEnum.Key;
            }

            TreeNode prop = source.Nodes.Add("Properties");
            prop.ImageIndex = (int)IconsEnum.CloseFolser;

            foreach (System.Collections.Generic.KeyValuePair<string, EDMProperty> p in entity.Properties)
            {
                prop.Nodes.Add(p.Key).ImageIndex = (int)IconsEnum.Property;
            }

            TreeNode nav = source.Nodes.Add("Navigation Properties");
            nav.ImageIndex = (int)IconsEnum.CloseFolser;

            foreach (System.Collections.Generic.KeyValuePair<string, Entity> n in entity.NavigationProperties)
            {
                nav.Nodes.Add(n.Key).ImageIndex = (int)IconsEnum.Nav;
            }
        }
    }
}
