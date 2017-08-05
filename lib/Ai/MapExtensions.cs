using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using lib.Structures;
using lib.viz;
using lib.viz.Detalization;
using MoreLinq;

namespace lib.Ai
{
    public static class MapExtensions
    {
        public static void ShowWithPath(this Map map, int[] pathSiteIds)
        {
            var form = new Form()
            {
                Text = pathSiteIds.ToDelimitedString(" - "),
                WindowState = FormWindowState.Maximized
            };
            var panel = new ScaledViewPanel(new MapPainter { Map = map, PainterAugmentor = new PathAugmentor(pathSiteIds, new DefaultPainterAugmentor()) })
            {
                Dock = DockStyle.Fill
            };
            form.Controls.Add(panel);
            form.ShowDialog();
        }
    }
    public class PathAugmentor : IPainterAugmentor
    {
        private readonly IList<int> path;
        private readonly IPainterAugmentor fallbackAugmentor;

        public PathAugmentor(IList<int> path, IPainterAugmentor fallbackAugmentor)
        {
            this.path = path;
            riversPath = path.Pairwise((a, b) => new River(a, b)).ToHashSet();
            this.fallbackAugmentor = fallbackAugmentor;
        }

        private IndexedMap map;
        private HashSet<River> riversPath;

        public IndexedMap Map
        {
            get { return map; }
            set
            {
                map = value;
                fallbackAugmentor.Map = value;
            }
        }

        public SitePainterData GetData(Site site)
        {
            return fallbackAugmentor.GetData(site);
        }

        public RiverPainterData GetData(River river)
        {
            if (!riversPath.Contains(river)) return fallbackAugmentor.GetData(river);
            return new RiverPainterData
            {
                Color = Color.DarkViolet,
                PenWidth = 4
            };
        }

        public FuturePainterData GetData(int punderId, Future future)
        {
            return fallbackAugmentor.GetData(punderId, future);
        }

        public bool ShowFutures { get; set; }
    }
}