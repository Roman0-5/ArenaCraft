using UnityEngine.UIElements;

namespace ArenaCraft
{
    public static class ResponsiveUILayout
    {
        public const string CompactClass = "layout-compact";
        public const string NarrowClass = "layout-narrow";
        public const string ShortClass = "layout-short";

        public static void Attach(VisualElement root)
        {
            if (root == null) return;

            root.UnregisterCallback<GeometryChangedEvent>(HandleGeometryChanged);
            root.RegisterCallback<GeometryChangedEvent>(HandleGeometryChanged);
            Apply(root, root.resolvedStyle.width, root.resolvedStyle.height);
        }

        private static void HandleGeometryChanged(GeometryChangedEvent evt)
        {
            if (evt.currentTarget is VisualElement root)
                Apply(root, evt.newRect.width, evt.newRect.height);
        }

        private static void Apply(VisualElement root, float width, float height)
        {
            root.EnableInClassList(CompactClass, width < 1000f || height < 720f);
            root.EnableInClassList(NarrowClass, width < 720f);
            root.EnableInClassList(ShortClass, height < 560f);
        }
    }
}
