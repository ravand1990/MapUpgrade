using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using MapUpgrade.Utils;
using PoeHUD.Framework;
using PoeHUD.Framework.Helpers;
using PoeHUD.Models;
using PoeHUD.Models.Enums;
using PoeHUD.Plugins;
using PoeHUD.Poe.Components;
using PoeHUD.Poe.RemoteMemoryObjects;
using SharpDX;
using SharpDX.Direct3D9;
using System.Security.Cryptography;
using System.Text;

namespace MapUpgrade
{
    internal class MapUpgrade : BaseSettingsPlugin<Settings>
    {
        private readonly IngameState ingameState;
        private bool isBusy;
        private MD5 md5Hasher = MD5.Create();
        private long currentStash;
        private List<Tuple<string, RectangleF>> maps = new List<Tuple<string, RectangleF>>();
        static Thread getMapsThread;

        public MapUpgrade()
        {
            ingameState = GameController.Game.IngameState;
            PluginName = "MapUpgrade";
        }

        public override void Initialise()
        {
            base.Initialise();
            getMapsThread = new Thread(getInventoryMaps);
            getMapsThread.Start();
        }

        public override void Render()
        {
            if (!Settings.Enable)
                return;

            debug();

            indicateMapPairs();

            if (WinApi.IsKeyDown(Settings.HotKey) && !isBusy)
            {
                isBusy = true;
                transferMapPairs();
                isBusy = false;
            }


        }

        public override void EntityAdded(EntityWrapper entityWrapper)
        {
            base.EntityAdded(entityWrapper);
        }

        public override void EntityRemoved(EntityWrapper entityWrapper)
        {
            base.EntityRemoved(entityWrapper);
        }

        public override void OnClose()
        {
            base.OnClose();
        }

        private void getInventoryMaps()
        {
            while (true) { 
            if (!ingameState.ServerData.StashPanel.IsVisible)
            {
                return;
            }
            maps = new List<Tuple<string, RectangleF>>();

            var visibleStash = ingameState.ServerData.StashPanel.VisibleStash;
            var i = 0;
            foreach (var item in visibleStash.VisibleInventoryItems)
            {
                var itemBase = GameController.Files.BaseItemTypes.Translate(item.Item.Path);
                var mapTier = item.Item.GetComponent<Map>().Tier;
                var itemMods = item.Item.GetComponent<Mods>();        

               // if(i==0) LogError(itemBase.BaseName+":"+mark, 10);


                i++;
                var itemClass = "";
                try
                {
                    itemClass = itemBase.ClassName;
                }
                catch (Exception e)
                {
                }


                if (itemClass.Equals("Map") && mapTier <= Settings.Tier && itemMods.ItemRarity != ItemRarity.Unique)
                {
                    var rect = item.GetClientRect();
                    var map = new Tuple<string, RectangleF>(itemBase.BaseName, rect);
                    maps.Add(map);
                }
            }
            Thread.Sleep(1000);
            }
        }


        private void indicateMapPairs()
        {
            if (!ingameState.IngameUi.InventoryPanel.IsVisible) return;
            var thisStash = ingameState.ServerData.StashPanel.VisibleStash.Address;

            //if (currentStash != thisStash)
            //{
            //    getInventoryMaps();
            //}
      
            if (maps == null)
            {
                return;
            }
            var foundTuples = maps.GroupBy(c => c.Item1).Where(g => g.Skip(1).Any() && g.Count() >= 3)
                .SelectMany(c => c).OrderBy(c => c.Item1).ToList();


            var i = 0;
            var j = 0;
            foreach (var map in foundTuples)
            {

                String r = map.Item1[0].ToString();
                String g = map.Item1[map.Item1.Length - 5].ToString();
                String b = map.Item1[(map.Item1.Length - 5)/2].ToString(); ;

                var hashedR = md5Hasher.ComputeHash(Encoding.UTF8.GetBytes(r));
                var hashedG = md5Hasher.ComputeHash(Encoding.UTF8.GetBytes(g));
                var hashedB = md5Hasher.ComputeHash(Encoding.UTF8.GetBytes(b));

                var iR = Math.Abs(BitConverter.ToInt32(hashedR, 0)) % 256;
                var iG = Math.Abs(BitConverter.ToInt32(hashedG, 0)) % 256;
                var iB = Math.Abs(BitConverter.ToInt32(hashedB, 0)) % 256;

                int offset = 6;
                RectangleF rect = new RectangleF((int)map.Item2.X+offset/2,(int)map.Item2.Y+offset/2,map.Item2.Width-offset,map.Item2.Height-offset);
                Graphics.DrawFrame(rect, 2, new Color(iR,iG,iB));
                if (j < foundTuples.Count - 1 && foundTuples[j].Item1 != foundTuples[j + 1].Item1)
                    i++;
                j++;
            }
            currentStash = thisStash;
        }

        private Dictionary<string, int> getMapPairs()
        {
            //getInventoryMaps();

            var mapCount = new Dictionary<string, int>();
            foreach (var key in maps)
                if (!mapCount.ContainsKey(key.Item1))
                    mapCount.Add(key.Item1, 1);
                else
                    mapCount[key.Item1]++;
            return mapCount;
        }

        private void transferMapPairs()
        {
            var mapCount = getMapPairs();
            var prevMousePosition = Mouse.GetCursorPosition();
            //getInventoryMaps();

            foreach (var key in mapCount)
                if (key.Value >= 3)
                {
                    var foundTuples = maps.Where(map => map.Item1 == key.Key).ToList();

                    for (var count = foundTuples.Count; count > 0;)
                    {
                        moveMaps(foundTuples[count - 1].Item2.Center);
                        count--;
                        moveMaps(foundTuples[count - 1].Item2.Center);
                        count--;
                        moveMaps(foundTuples[count - 1].Item2.Center);
                        count--;

                        if (count < 3)
                            break;
                    }
                }
            Mouse.moveMouse(prevMousePosition);
        }

        private void moveMaps(Vector2 itemPosition)
        {
            Keyboard.HoldKey((byte) Keys.LControlKey);
            Thread.Sleep(Mouse.DELAY_MOVE);
            Mouse.moveMouse(itemPosition);
            Mouse.LeftUp(Settings.Speed);
            Thread.Sleep(Mouse.DELAY_MOVE);
            Keyboard.ReleaseKey((byte) Keys.LControlKey);
        }

        private void debug()
        {
        }
    }
}