﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using NUnit.Framework;

namespace lib
{
    internal class AiFactory
    {
        private readonly Func<IAi> create;
        public string Name;

        public AiFactory(string name, Func<IAi> create)
        {
            Name = name;
            this.create = create;
        }

        public IAi Create()
        {
            return create();
        }

        public override string ToString()
        {
            return Name;
        }
    }

    internal class StartGameConfigPanel : Panel
    {
        private readonly ListBox allAisList;
        private readonly ListBox mapsList;

        public StartGameConfigPanel()
        {
            var mapsListLabel = new Label
            {
                Dock = DockStyle.Top,
                Text = "Maps list. Click to select."
            };
            mapsList = new ListBox
            {
                Dock = DockStyle.Fill
            };
            mapsList.SelectedValueChanged += (sender, args) =>
            {
                MapChanged?.Invoke((NamedMap) mapsList.SelectedItem);
            };
            var allAisListLabel = new Label
            {
                Dock = DockStyle.Bottom,
                Text = "AIs list. Double click to add AI."
            };
            allAisList = new ListBox
            {
                Dock = DockStyle.Bottom,
                Height = 200
            };
            allAisList.DoubleClick += (sender, args) => { AiSelected?.Invoke((AiFactory) allAisList.SelectedItem); };
            var selectedAisListLabel = new Label
            {
                Dock = DockStyle.Bottom,
                Text = "Selected AIs list. Double click to remove."
            };
            var selectedAisList = new ListBox
            {
                Dock = DockStyle.Bottom,
                Height = 100
            };
            selectedAisList.DoubleClick += (sender, args) =>
            {
                AiAtIndexRemoved?.Invoke(selectedAisList.SelectedIndex);
            };
            AiSelected += factory =>
            {
                var ai = factory.Create();
                SelectedAis.Add(ai);
                selectedAisList.Items.Add(ai);
            };
            AiAtIndexRemoved += index =>
            {
                selectedAisList.Items.RemoveAt(index);
                SelectedAis.RemoveAt(index);
            };
            MapChanged += map => { SelectedMap = map; };
            Controls.Add(mapsListLabel);
            Controls.Add(mapsList);
            Controls.Add(allAisListLabel);
            Controls.Add(allAisList);
            Controls.Add(selectedAisListLabel);
            Controls.Add(selectedAisList);
        }

        public List<IAi> SelectedAis { get; } = new List<IAi>();
        public NamedMap SelectedMap { get; private set; }

        public event Action<NamedMap> MapChanged;
        public event Action<AiFactory> AiSelected;
        public event Action<int> AiAtIndexRemoved;

        public void SetMaps(NamedMap[] maps)
        {
            mapsList.Items.AddRange(maps.Cast<object>().ToArray());
        }

        public void SetAis(params AiFactory[] ais)
        {
            allAisList.Items.AddRange(ais.Cast<object>().ToArray());
        }
    }

    public class StartGameConfigPanel_Tests
    {
        [Explicit]
        [Test]
        [STAThread]
        public void Show()
        {
            var form = new Form()
            {
                Size = new Size(300, 800)
            };
            var panel = new StartGameConfigPanel
            {
                Dock = DockStyle.Fill
            };
            panel.SetMaps(MapLoader.LoadDefaultMaps().ToArray());
            panel.SetAis(new AiFactory("Basic", () => new JunkAi()));
            panel.MapChanged += map => form.Text = map.Name;
            panel.AiSelected += factory => form.Text = factory.Name;
            form.Controls.Add(panel);
            form.ShowDialog();
        }

        private class JunkAi : IAi
        {
            public string Name => "Junk";

            public void StartRound(int punterId, int puntersCount, Map map)
            {
                throw new NotImplementedException();
            }

            public IMove GetNextMove(IMove[] prevMoves, Map map)
            {
                throw new NotImplementedException();
            }

            public string SerializeGameState()
            {
                throw new NotImplementedException();
            }

            public void DeserializeGameState(string gameState)
            {
                throw new NotImplementedException();
            }

            public override string ToString()
            {
                return Name;
            }
        }
    }
}