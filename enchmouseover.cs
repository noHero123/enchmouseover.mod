using System;

using ScrollsModLoader.Interfaces;
using UnityEngine;
using Mono.Cecil;
//using Mono.Cecil;
//using ScrollsModLoader.Interfaces;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Reflection;
using JsonFx.Json;
using System.Text.RegularExpressions;



namespace enchmouseover.mod
{
    struct enchantments
    {
        public string name;
        public int posx;
        public int posy;
        
    }

    public class moveunit
    {
        public int move = 0;
        public TileColor tp = TileColor.unknown;
        public int movesdone = 0;

        public moveunit(int m, TilePosition p)
        {
            this.move = m;
            this.tp = p.color;
            
        }

    }


    public class enchmouseover : BaseMod, ICommListener
	{
        private BattleMode bm = null;
        private Boolean showpicture = false;
        private Boolean showall = false;
        private Boolean allwayson = false;
        List<enchantments> enchlist = new List<enchantments>();
        List<enchantments> Allenchlist = new List<enchantments>();
        Dictionary<string, Texture2D> enchantslib= new Dictionary<string, Texture2D>();
         int picx=53;
        int picy=40;
        private int[] cardids;
        private string[] cardnames;
        private int[] cardImageid;
        private int showpattern=2;

        private FieldInfo reftile;
        private FieldInfo tileover;
        private FieldInfo mrktpe;
        private FieldInfo sbfrm;
        private FieldInfo chargeAnim = typeof(Tile).GetField("chargeAnim", BindingFlags.Instance | BindingFlags.NonPublic);
        private FieldInfo targetAnimBack = typeof(Tile).GetField("targetAnimBack", BindingFlags.Instance | BindingFlags.NonPublic);
        private FieldInfo targetAnimFront = typeof(Tile).GetField("targetAnimFront", BindingFlags.Instance | BindingFlags.NonPublic);

        private int cardnametoimageid(string name) { return cardImageid[Array.FindIndex(cardnames, element => element.Equals(name))]; }
        private FieldInfo buffMaterialInfo = typeof(Unit).GetField("buffMaterial", BindingFlags.Instance | BindingFlags.NonPublic);
        MethodInfo updatechat = typeof(BattleMode).GetMethod("updateChat", BindingFlags.NonPublic | BindingFlags.Instance);
        MethodInfo mi=typeof(BattleMode).GetMethod("getAllUnitsCopy", BindingFlags.NonPublic | BindingFlags.Instance);
        MethodInfo hm = typeof(BattleMode).GetMethod("handleGameChatMessage", BindingFlags.NonPublic | BindingFlags.Instance);

        MethodInfo selectTargetMethod = typeof(BattleMode).GetMethod("selectTargetArea", BindingFlags.NonPublic | BindingFlags.Instance);

        TileColor showLobbers = TileColor.unknown;
        List<TilePosition> enemyLobbers = new List<TilePosition>();
        TilePosition currentHover = new TilePosition(TileColor.white, 1, 1);

        Dictionary<long, moveunit> moveCounter = new Dictionary<long, moveunit>();

        private FieldInfo attackcounterinfo = typeof(Unit).GetField("attackCounter", BindingFlags.Instance | BindingFlags.NonPublic);
        private FieldInfo attackCounterObjArrinfo = typeof(Unit).GetField("attackCounterObjArr", BindingFlags.Instance | BindingFlags.NonPublic);
        private FieldInfo countdownStatinfo = typeof(Unit).GetField("countdownStat", BindingFlags.Instance | BindingFlags.NonPublic);
        private MethodInfo createSymbolsMethodInfo = typeof(Unit).GetMethod("createSymbols", BindingFlags.NonPublic | BindingFlags.Instance);
        
        bool mark = false;

        public void onConnect(OnConnectData ocd)
        { 
        //lol
        }
        public void handleMessage(Message msg)
        { // collect data for enchantments (or units who buff)

            if (msg is CardTypesMessage)
            {

                JsonReader jsonReader = new JsonReader();
                Dictionary<string, object> dictionary = (Dictionary<string, object>)jsonReader.Read(msg.getRawText());
                Dictionary<string, object>[] d = (Dictionary<string, object>[])dictionary["cardTypes"];
                this.cardids = new int[d.GetLength(0)];
                this.cardnames = new string[d.GetLength(0)];
                this.cardImageid = new int[d.GetLength(0)];

                for (int i = 0; i < d.GetLength(0); i++)
                {
                    cardids[i] = Convert.ToInt32(d[i]["id"]);
                    cardnames[i] = d[i]["name"].ToString();
                    cardImageid[i] = Convert.ToInt32(d[i]["cardImage"]);
                }

                //App.Communicator.removeListener(this);//dont need the listener anymore
            }

            if(msg is AbilityInfoMessage)
            {
                AbilityInfoMessage aim = (AbilityInfoMessage)msg;
                if (aim.abilityId == "Move")//aim.isPlayable &&
                {
                    this.showLobbers = TileColor.white;
                    if (aim.unitPosition.color == TileColor.white) this.showLobbers = TileColor.black;

                    
                    this.calculateEnemyLobbers(this.showLobbers);

                }
                this.showLobbers = TileColor.unknown;
            }

            if (msg is CardInfoMessage)
            {
                CardInfoMessage aim = (CardInfoMessage)msg;
                //Console.WriteLine("cardinfo");
                if (aim.card.getPieceKind() == CardType.Kind.CREATURE || aim.card.getPieceKind() == CardType.Kind.STRUCTURE)//aim.isPlayable &&
                {
                    this.mark =true;
                    if (aim.data.selectableTiles.Count >= 1)
                    {
                        //Console.WriteLine("cardinfo " + aim.data.selectableTiles.tileSets[0][0].color);
                        this.showLobbers = aim.data.selectableTiles.tileSets[0][0].color.otherColor();
                        this.calculateEnemyLobbers(this.showLobbers);
                    }

                }
                this.showLobbers = TileColor.unknown;
            }


            return;
        }


        private void calculateEnemyLobbers(TileColor tc)
        {
            this.enemyLobbers.Clear();
            //this.bm.getTileList(tc);
            //Console.WriteLine("##");
            for (int i = 0; i < 5; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    Unit unitFromTile = this.bm.getUnit(tc, i, j);
                    if (unitFromTile == null) continue;
                    /*int movecount = 0;
                    foreach (ActiveAbility aa in unitFromTile.getActiveAbilities())
                    {
                        Console.WriteLine(aa.name);
                        if (aa.isMoveLike()) movecount++;
                    }
                    Console.WriteLine("-");*/
                    TilePosition tp2 = new TilePosition(tc.otherColor(), i, 2 - j);

                    if (unitFromTile.getTargetArea() == TargetArea.RADIUS_4)
                    {
                        selectTargetMethod.Invoke(this.bm, new object[]{TargetArea.RADIUS_4, tp2});
                        selectTargetArea(TargetArea.RADIUS_4, tp2);
                    }
                    if (unitFromTile.getTargetArea() == TargetArea.RADIUS_3)
                    {
                        selectTargetMethod.Invoke(this.bm, new object[] { TargetArea.RADIUS_3, tp2 });
                        selectTargetArea(TargetArea.RADIUS_3, tp2);
                    }
                    /*if (unitFromTile.getTargetArea() == TargetArea.RADIUS_7)
                    {
                        TilePosition tp2 = new TilePosition(tc.otherColor(), i, 2 - j);
                        selectTargetMethod.Invoke(this.bm, new object[] { TargetArea.RADIUS_7, tp2 });
                    }*/

                }
            }

        }

        private void selectTargetArea(TargetArea targetArea, TilePosition tp)
        {
            Tile.SelectionType markedID = targetArea.selectionType();
            List<TilePosition> list = targetArea.getTargets(tp);
            List<Tile> tiles = new List<Tile>();
            foreach (TilePosition current2 in list)
            {
                this.enemyLobbers.Add(current2); 
            }
        }

        public void writetxtinchat(string msgs)
        {

            Console.WriteLine(msgs);
            try
            {
                String chatMsg = msgs;

                if (hm != null) // send chat message
                {
                    GameChatMessageMessage gcmm = new GameChatMessageMessage(chatMsg);
                    gcmm.from = "Enchmouseover";
                    hm.Invoke(this.bm, new GameChatMessageMessage[] { gcmm });
                }
                else // can't invoke updateChat
                {
                }

                /*if (updatechat != null) // send chat message
                {

                    updatechat.Invoke(this.bm, new String[] { chatMsg });
                }
                else // can't invoke updateChat
                {
                }*/
            }
            catch // could not get information
            {
            }


        }


		//initialize everything here, Game is loaded at this point4
        public enchmouseover()
		{

            mi = typeof(BattleMode).GetMethod("getAllUnitsCopy", BindingFlags.NonPublic | BindingFlags.Instance);
            reftile = typeof(Tile).GetField("referenceTile", BindingFlags.Instance | BindingFlags.NonPublic);
            tileover = typeof(Tile).GetField("tileOverlay", BindingFlags.Instance | BindingFlags.NonPublic);
            mrktpe = typeof(Tile).GetField("markerType", BindingFlags.Instance | BindingFlags.NonPublic);
            sbfrm = typeof(Tile).GetField("subFrame", BindingFlags.Instance | BindingFlags.NonPublic);
            try
            {
                App.Communicator.addListener(this);
            }
            catch { }
            Console.WriteLine("lodet enchmouseover ####################################");
    
		}



		public static string GetName ()
		{
            return "Enchantmouseover";
		}

		public static int GetVersion ()
		{
			return 10;
		}

        private void Allenchantcreator(List<EnchantmentInfo> enchants, Tile component, Boolean all) // creates global/local enchantlist
        {
            if (enchants.Count > 0)
            {
                enchantments tmpench = new enchantments();

                //circle with screen coordinates
                      /* Vector3 screenPoint = Camera.main.WorldToScreenPoint(component.transform.position);
                       double mx=screenPoint.x;
                       double my=Camera.main.pixelHeight-screenPoint.y;
                       int anz= enchants.Count ;
                       Double winkel = 3.1415926536*2/(double)anz ;
                       Double radius =80;
                       Console.WriteLine(component.transform.position.ToString() + " #####");
                       for (int i=0; i < anz; i++) 
                       {
                               tmpench.name=enchants.ElementAt(i).name;
                               tmpench.posx = (int)(mx + Math.Cos(i*winkel)*radius)- picx/2;
                               tmpench.posy = (int)(my + Math.Sin(i*winkel)*radius)-picy/2;
                               enchlist.Add(tmpench);
                               if (!enchantslib.ContainsKey(tmpench.name))
                               {
                                   Texture2D newtexture = new Texture2D(2, 2, TextureFormat.RGB24, false);
                                   byte[] data = File.ReadAllBytes("C:\\Users\\Thele\\AppData\\Local\\Mojang\\Scrolls\\game\\Bear Paw_prev.png");
                                   newtexture.LoadImage(data);
                                   enchantslib.Add(tmpench.name, newtexture);
                               }
                              
                       }*/

                // circle with real world coordinates
                Vector3 screenPoint = component.transform.position;//
                double mx = screenPoint.x;
                double my = screenPoint.z;
                float tmpx, tmpz;
                double tempx, tempy;
                int anz = enchants.Count;
                int leftx = 0, rightx = 0, y = 0, belowposx = 0, belowposy = 0;
                Double winkel = 3.1415926536 * 2 / (double)anz;
                Double radius = 1.0;
                if (this.showpattern == 2)
                {
                    winkel = 3.1415926536 * 2;
                    tmpx = (float)((mx - 0.08 + Math.Cos(winkel) * radius));
                    tmpz = (float)((my + Math.Sin(winkel) * radius));
                    tempx = Camera.main.WorldToScreenPoint(new Vector3(tmpx, 0f, tmpz)).x;
                    tempy = Camera.main.WorldToScreenPoint(new Vector3(tmpx, 0f, tmpz)).y;
                    belowposx = (int)(tempx - picx / 2);
                    belowposy = (int)(Camera.main.pixelHeight - tempy - picy / 2);
                    leftx = belowposx;
                    rightx = belowposx + 2 + picx;
                    y = belowposy;
                }
                for (int i = 0; i < anz; i++)
                {
                    tmpench.name = enchants.ElementAt(i).name;
                    if (this.showpattern == 1)// circle with real world coordinates
                    {
                        tmpx = (float)((mx - 0.08 + Math.Cos(i * winkel) * radius));
                        tmpz = (float)((my + Math.Sin(i * winkel) * radius));
                        tempx = Camera.main.WorldToScreenPoint(new Vector3(tmpx, screenPoint.y, tmpz)).x;
                        tempy = Camera.main.WorldToScreenPoint(new Vector3(tmpx, screenPoint.y, tmpz)).y;
                        tmpench.posx = (int)(tempx - picx / 2);
                        tmpench.posy = (int)(Camera.main.pixelHeight - tempy - picy / 2);
                    }
                    if (this.showpattern == 2)// right side with real world coordinates
                    {
                        tmpench.posy = y;
                        if ((i % 5) % 2 == 0)
                        {
                            tmpench.posx = leftx;
                            leftx = leftx - 2 - picx;
                        }
                        else
                        {
                            tmpench.posx = rightx;
                            rightx = rightx + 2 + picx;
                        }

                        if ((i + 1) % 5 == 0)// after 5 enchantments, go one line deeper
                        {
                            y = y + 2 + picy;
                            leftx = belowposx;
                            rightx = belowposx + 2 + picx;
                        };
                    }
                    if (all == true)
                    {
                        this.Allenchlist.Add(tmpench);
                    }
                    else 
                    { 
                        this.enchlist.Add(tmpench); 
                    }
                    if (!enchantslib.ContainsKey(tmpench.name))
                    {
                        string enchname = tmpench.name;
                        if (enchname == ("Poison Immunity")) { enchname = "Sand Pact Memorial"; };
                        if (enchname.StartsWith("Resistance")) { enchname = "Stone Pact Memorial"; };
                        if (enchname.StartsWith("Poison")) { enchname = "Ranger's Bane"; };
                        if (enchname.StartsWith("Curse")) { enchname = "Cluster Hex"; };
                        if (enchname == "Decomposing") { enchname = "Return To Nature"; };
                        if (enchname == "Piercing") { enchname = "Piercing Projectile"; };
                        if (enchname.StartsWith("Armor")) { enchname = "Wings Shield"; };
                        if (enchname.StartsWith("Magic Armor")) { enchname = "Metempsychosis"; };
                        if (enchname == "Slayer") { enchname = "Ilmire Hunter"; };
                        if (enchname.StartsWith("Move")) { enchname = "New Orders"; };
                        if (enchname.StartsWith("Haste")) { enchname = "Speed"; };
                        if (enchname.StartsWith("Spiky")) { enchname = "Illthorn"; };
                        if (enchname == ("Inspired")) { enchname = "Jarl Urhald"; };
                        Texture2D newtexture = new Texture2D(2, 2, TextureFormat.RGB24, false);
                        try
                        {
                            newtexture = App.AssetLoader.LoadTexture2D(cardnametoimageid(enchname).ToString());
                        }
                        catch
                        {
                            this.writetxtinchat("Enchmod doesnt know " + enchname);
                        }
                        this.enchantslib.Add(tmpench.name, newtexture);
                    }
                }
            }

        }

        /*public void handleMessage(Message msg)
        {

            if (msg is RoomInfoMessage)
            {

                JsonReader jsonReader = new JsonReader();
                Dictionary<string, object> dictionary = (Dictionary<string, object>)jsonReader.Read(msg.getRawText());
                Dictionary<string, object>[] d = (Dictionary<string, object>[])dictionary["profiles"];
                string tmpid = "";
                string tmpname = "";
                for (int i = 0; i < d.GetLength(0); i++)
                {
                    tmpid=d[i]["id"].ToString();
                    tmpname = d[i]["name"].ToString();
                    playerinfo tmpplayer = new playerinfo();
                    tmpplayer.name = tmpname;
                    tmpplayer.ID = tmpid;

                    if (!liplayerinfo.Contains(tmpplayer)) { liplayerinfo.Add(tmpplayer); }
                }

                

            }
            
            return;
        }

        public void onReconnect()
        {
            return; // don't care
        }*/

       

		//only return MethodDefinitions you obtained through the scrollsTypes object
		//safety first! surround with try/catch and return an empty array in case it fails
		public static MethodDefinition[] GetHooks (TypeDefinitionCollection scrollsTypes, int version)
		{
            try
            {
                return new MethodDefinition[] {
                    scrollsTypes["BattleMode"].Methods.GetMethod("sendBattleRequest", new Type[]{typeof(Message)}),
                    scrollsTypes["BattleMode"].Methods.GetMethod("OnGUI")[0],
                    scrollsTypes["BattleMode"].Methods.GetMethod("Update")[0],

                    scrollsTypes["BattleMode"].Methods.GetMethod("tileOver", new Type[]{typeof(TilePosition)} ),
                    scrollsTypes["BattleMode"].Methods.GetMethod("tileOut", new Type[]{typeof(GameObject),typeof(int),typeof(int)} ),
                    scrollsTypes["BattleMode"].Methods.GetMethod("toggleUnitStats")[0],

                    scrollsTypes["Unit"].Methods.GetMethod("renderUnit")[0],

                    scrollsTypes["BattleMode"].Methods.GetMethod("markMoveTile", new Type[]{typeof(TilePosition),typeof(TilePosition)} ),
                    scrollsTypes["Tile"].Methods.GetMethod("markInternal", new Type[]{typeof(Tile.SelectionType),typeof(float)} ),

                    scrollsTypes["BattleMode"].Methods.GetMethod("forceRunEffect", new Type[]{typeof(EffectMessage)}),
                    
                    // scrollsTypes["BattlemodeUI"].Methods.GetMethod("Update")[0],
                    //scrollsTypes["Tile"].Methods.GetMethod("updateMoveAnim")[0],
                   //scrollsTypes["ChatUI"].Methods.GetMethod("OnGUI")[0],

             };
            }
            catch
            {
                return new MethodDefinition[] { };
            }
		}


        public long getId(Unit u)
        {

            return u.GetInstanceID();// better than cardid because token have the same ;_;
        }

        public void resetMoveAllUnits()
        {
            for (int i = 0; i < 5; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    Unit unitFromTile = this.bm.getUnit(TileColor.white, i, j);
                    if (unitFromTile != null)
                    {
                        updateMoveUnit(unitFromTile, unitFromTile.getBuffs(), unitFromTile.getActiveAbilities(), unitFromTile.getName(), unitFromTile.getTilePosition());
                    }

                    unitFromTile = this.bm.getUnit(TileColor.black, i, j);
                    if (unitFromTile != null)
                    {
                        updateMoveUnit(unitFromTile, unitFromTile.getBuffs(), unitFromTile.getActiveAbilities(), unitFromTile.getName(), unitFromTile.getTilePosition());
                    }
                }
            }
        }

        public void updateMoveUnit(Unit un, List<EnchantmentInfo> enchants, ActiveAbility[] actives, string name, TilePosition tp)
        {
            long id = this.getId(un);
            int hasmove = 0;
            foreach (ActiveAbility aa in actives)
            {
                if (aa.isMoveLike()) hasmove = 1;
            }
            if (name == "Infected Gravelock") hasmove = 0;//maybe at last?
            if (name == "Stormknight") hasmove = 2;//maybe at last?
            if (this.moveCounter.ContainsKey(id))
            {
                this.moveCounter[id].move = hasmove;
            }
            else
            {
                this.moveCounter.Add(id, new moveunit(hasmove, tp));
            }

            foreach (EnchantmentInfo ei in enchants)
            {
                if (ei.name == "Move") this.moveCounter[id].move++; // is maybe wrong (see trials-buffs)
                if (ei.name == "Binding Root") this.moveCounter[id].move -= 2;
                if (ei.name == "Dryadic Power") this.moveCounter[id].move -= 1;
                if (ei.name == "Horn of Ages") this.moveCounter[id].move -= 1;
                if (ei.name == "Malevolent Gaze") this.moveCounter[id].move -= 2;
                if (ei.name == "New Orders") this.moveCounter[id].move += 1;
                if (ei.name == "Nuru's Needle") this.moveCounter[id].move -= 1;
                //if (ei.name == "Oum Lasa High Guard") this.moveCounter[id] -= 100;
                if (ei.name == "Roasted Bean Potion") this.moveCounter[id].move += 1;
                if (ei.name == "Wings Captain") this.moveCounter[id].move += 1;
            }

            //oum Lasa high guard effect:
            //Console.WriteLine("" + tp.ToString());
            for (int i = 0; i < tp.column; i++)
            {
                Unit u = this.bm.getUnit(tp.color, tp.row, i);

                if (u != null)
                {
                    //Console.WriteLine("own " + u.name);
                    return;
                }
            }
            for (int i = 0; i < 3; i++)
            {
                Unit u = this.bm.getUnit(tp.color.otherColor(), tp.row, i);
                if (u != null)
                {
                    //Console.WriteLine("enemy " + u.name);
                    if (u.getName() == "Oum Lasa High Guard")
                    {
                        this.moveCounter[id].move = 0;
                    }
                    return;
                }
            }
        }

        public void printMove(TileColor tc)
        {
            foreach (KeyValuePair<long, moveunit> kvp in this.moveCounter)
            {
                if (kvp.Value.tp != tc) continue;

                Console.WriteLine(kvp.Key + " move: " + kvp.Value.movesdone + " " +kvp.Value.move + " " + kvp.Value.tp.ToString());
                
            }
 
        }

        public override bool WantsToReplace(InvocationInfo info)
        {
            if (info.targetMethod.Equals("sendBattleRequest"))
            {
                Console.WriteLine("sendrequest");
                if (info.arguments[0] is GameChatMessageMessage)
                {


                    GameChatMessageMessage msg = (GameChatMessageMessage)info.arguments[0];

                    string[] splitt = msg.text.Split(' ');

                    if ((splitt[0] == "/showench" || splitt[0] == "\\showench"))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public override void ReplaceMethod(InvocationInfo info, out object returnValue)
        {
            
            returnValue = null;

            if (info.targetMethod.Equals("sendBattleRequest"))
            {
                Console.WriteLine("sendrequest");
                if (info.arguments[0] is GameChatMessageMessage)
                {
                    

                    GameChatMessageMessage msg = (GameChatMessageMessage)info.arguments[0];

                    string[] splitt = msg.text.Split(' ');

                    if ((splitt[0] == "/showench" || splitt[0] == "\\showench"))
                    {
                        Boolean donesomething =false;
                        if (splitt.Length == 2)
                        {
                            if (splitt[1] == "circle")
                            {
                                this.showpattern = 1;
                                string text = "enchantments are shown on a circle around unit";
                                writetxtinchat(text);
                                donesomething = true;
                            }

                            if (splitt[1] == "below")
                            {
                                this.showpattern = 2;
                                string text = "enchantments are shown below unit";
                                writetxtinchat(text);
                                donesomething = true;
                            }
                            if (splitt[1] == "ctrlon")
                            {
                                this.allwayson = true;
                                string text = "if unit stats are shown permanently, enchantments are shown permanently, ";
                                writetxtinchat(text);
                                donesomething = true;
                            }
                            if (splitt[1] == "ctrloff")
                            {
                                this.allwayson = false;
                                string text = "if unit stats are shown permanently, enchantments aren't shown permanently, ";
                                writetxtinchat(text);
                                donesomething = true;
                            }


                        }
                        if (donesomething==false)
                        {
                            string text = "commands: circle, below";
                            writetxtinchat(text);
                        }
                       
                    }

                }
            }
        }

        TilePosition tpStatsupdate = null;

        public override void BeforeInvoke(InvocationInfo info)
        {

            if (info.target is BattleMode && info.targetMethod.Equals("forceRunEffect"))
            {
                EffectMessage currentEffect = (EffectMessage)info.arguments[0];

                string type = currentEffect.type;
                try
                {
                    if (type == "TurnBegin")
                    {
                        resetMoveAllUnits();

                        foreach (KeyValuePair<long, moveunit> mu in this.moveCounter)
                        {
                            mu.Value.movesdone = 0;
                        }
                    }

                    

                    if (type == "UnitActivateAbility")
                    {
                        EMUnitActivateAbility cef = (EMUnitActivateAbility)currentEffect;
                        string name = cef.name;
                        if (name == "Move" || name == "Flying")
                        {

                            long id = this.getId(this.bm.getUnit(cef.unit));

                            if (this.moveCounter.ContainsKey(id)) this.moveCounter[id].movesdone += 1;
                        }
                    }

                    if (type == "StatsUpdate")
                    {
                        EMStatsUpdate cef = (EMStatsUpdate)currentEffect;
                        this.tpStatsupdate = new TilePosition( cef.target.color, cef.target.row, cef.target.column);
                    }



                    

                }
                catch
                {
                }
            }
            return;

        }

        private void showMoveOfUnit(Unit u)
        {

            List<GameObject> attackCounterObjArr = (List<GameObject>)attackCounterObjArrinfo.GetValue(u);
            Stat countdownStat = (Stat)countdownStatinfo.GetValue(u);

            int value = 0;
            long id= this.getId(u);
            if (this.moveCounter.ContainsKey(id))
            {
                moveunit mu = this.moveCounter[id];
                value = Math.Max(0, mu.move - mu.movesdone);
            }

            attackCounterObjArr.ForEach(delegate(GameObject g)
            {
                UnityEngine.Object.Destroy(g);
            });
            attackCounterObjArr.Clear();
            attackCounterObjArr.AddRange((List<GameObject>)this.createSymbolsMethodInfo.Invoke(u, new object[] { value, countdownStat.digits.transform }));
            attackCounterObjArr.ForEach(delegate(GameObject g)
            {
                g.tag = "blinkable_countdown";
            });

            attackCounterObjArr.ForEach(delegate(GameObject g)
            {
                g.renderer.material.color = Color.green;
                if (value == 0) g.renderer.material.color = Color.red;
            });

        }

        public override void AfterInvoke (InvocationInfo info, ref object returnValue)
        {

            if (info.target is BattleMode && info.targetMethod.Equals("forceRunEffect"))
            {
                if (tpStatsupdate != null)
                {
                    Unit statsupdate = this.bm.getUnit(tpStatsupdate);
                    if (statsupdate != null)
                    {
                        updateMoveUnit(statsupdate, statsupdate.getBuffs(), statsupdate.getActiveAbilities(), statsupdate.getName(), statsupdate.getTilePosition());
                    }
                    tpStatsupdate = null;
                }

                EffectMessage currentEffect = (EffectMessage)info.arguments[0];
                string type = currentEffect.type;

                if (type == "MoveUnit")
                {
                    this.resetMoveAllUnits();

                }

                if (type == "SummonUnit")
                {
                    EMSummonUnit cef = (EMSummonUnit)currentEffect;
                    Unit un = this.bm.getUnit(cef.target);

                    this.updateMoveUnit(un, new List<EnchantmentInfo>(), cef.card.getActiveAbilities(), cef.card.getName(), cef.target);

                }

                if (type == "TeleportUnits")
                {
                    this.resetMoveAllUnits();
                }
            }

            if (info.target is BattleMode && info.targetMethod.Equals("toggleUnitStats"))
            {
                Boolean showUnitStats= (Boolean)typeof(BattleMode).GetField ("showUnitStats", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(info.target);
                Allenchlist.Clear();
                this.showall = showUnitStats;
                if (showUnitStats == true)
                {
                    MethodInfo mi = typeof(BattleMode).GetMethod("getAllUnitsCopy", BindingFlags.NonPublic | BindingFlags.Instance);
                    foreach (Unit current in (List<Unit>)mi.Invoke(this.bm,null))
                    {  
                        List<EnchantmentInfo> enchants = current.getBuffs();
                        //Tile component = ((BattleMode)info.target).getTileFromUnit(current);
                        Tile component = ((BattleMode)info.target).getTile(current.getTilePosition());
                        this.Allenchantcreator(enchants, component, true);
              
                    }

                }
            }

            if (info.target is Unit && info.targetMethod.Equals("renderUnit"))
            {
                if (bm == null) return;
                Unit current = (Unit)info.target;
                List<EnchantmentInfo> enchants = current.getBuffs();
                if (enchants.Count > 0)
                {
                    //Tile component = ((BattleMode)bm).getTileFromUnit(current);
                    Tile component = ((BattleMode)bm).getTile(current.getTilePosition());
                    Tile.SelectionType marker = (Tile.SelectionType)mrktpe.GetValue(component);
                    if (marker != Tile.SelectionType.Hover)
                    {
                        
                        Material buffmat = (Material)this.buffMaterialInfo.GetValue(info.target);
                        buffmat.SetColor("_Color", ColorUtil.FromInts(255, 0, 0, 220));
                        buffmat.color = new Color(1.0f, 0.0f, 0.0f, 0.7f);
                        //current.renderer.material.color = new Color(1.0f, 0.5f, 0.5f, 0.7f);
                        //GameObject refer = (GameObject)reftile.GetValue(component);
                        //refer.renderer.material.color = new Color(1.0f, 0f, 0f, 0.4f);
                        //GameObject to = (GameObject)tileover.GetValue(component);
                        //to.renderer.material.color = new Color(1.0f, 0f, 0f, 0.4f);

                        //component.getReference().renderer.material.color = new Color(1f, 0f, 0f, 0.4f);
                    }
                }
            }

            if (info.target is BattleMode && info.targetMethod.Equals("Update"))
            {

                if (Input.GetKeyDown(KeyCode.M))
                {
                    foreach (Unit u in this.bm.getUnitsFor(TileColor.white))
                    {
                        showMoveOfUnit(u);
                    }
                    foreach (Unit u in this.bm.getUnitsFor(TileColor.black))
                    {
                        showMoveOfUnit(u);
                    }
                }

                if (Input.GetKeyUp(KeyCode.M))
                {

                    foreach (Unit u in this.bm.getUnitsFor(TileColor.white))
                    {
                        int v = (int)attackcounterinfo.GetValue(u);
                        u.setAttackCounter(v - 1);
                        u.setAttackCounter(v);
                    }
                    foreach (Unit u in this.bm.getUnitsFor(TileColor.black))
                    {
                        int v = (int)attackcounterinfo.GetValue(u);
                        u.setAttackCounter(v - 1);
                        u.setAttackCounter(v);
                    }



                }

            }

            if (info.target is BattleMode && info.targetMethod.Equals("OnGUI"))
            {
                if (bm == null)
                {
                    bm = (BattleMode)info.target;
                }

                if (Input.GetMouseButtonDown(0) && this.currentHover.color != TileColor.unknown)
                {
                    //mouse pressed!
                    this.calculateEnemyLobbers(this.currentHover.color.otherColor());

                    //Console.WriteLine("moves:###############################");
                    //printMove(TileColor.white);
                    //printMove(TileColor.black);
                }


                if (showall == true && this.allwayson==true)
                {

                    Allenchlist.Clear();
                    
                    foreach (Unit current in (List<Unit>)mi.Invoke(this.bm, null))
                    {
                        List<EnchantmentInfo> enchants = current.getBuffs();
                        //Tile component = ((BattleMode)info.target).getTileFromUnit(current);
                        Tile component = ((BattleMode)info.target).getTile(current.getTilePosition());
                        this.Allenchantcreator(enchants, component, true);
                    }

                    foreach (var item in Allenchlist)
                    {

                        Rect position = new Rect(item.posx, item.posy, picx, picy);
                        GUI.DrawTexture(position, enchantslib[item.name]);
                    }
                }
                else
                {
                    if (showpicture == true)
                    {
                      
                        foreach (var item in enchlist)
                        {

                            Rect position = new Rect(item.posx, item.posy, picx, picy);
                            GUI.DrawTexture(position, enchantslib[item.name]);
                        }

                    }
                }

                /*
                foreach (Unit current in (List<Unit>)mi.Invoke(this.bm, null))
                {
                    //current.renderer.material.color = new Color(1f, 1f, 1f, 1f);
                    Material buffmat = (Material)this.buffMaterialInfo.GetValue(current);
                    Console.WriteLine("#clolor12" + buffmat.GetColor("_Color"));
                    buffmat.SetColor("_Color", ColorUtil.FromHex24(16777173u));
                    List<EnchantmentInfo> enchants = current.getBuffs();
                    Tile component = ((BattleMode)info.target).getTileFromUnit(current);
                    if (enchants.Count > 0)
                    {
                        Tile.SelectionType marker = (Tile.SelectionType)mrktpe.GetValue(component);
                        if (marker != Tile.SelectionType.Hover)
                        {
                            //current.renderer.material.color = new Color(1.0f, 0.5f, 0.5f, 0.7f);
                            buffmat.SetColor("_Color", ColorUtil.FromInts(255,0,0,220));
                            buffmat.color = new Color(1.0f, 0.0f, 0.0f, 0.7f);
                            current.renderer.material.color = new Color(1.0f, 0.0f, 0.0f, 0.7f);
                            Console.WriteLine("#clolorrrrr" + buffmat.GetColor("_Color"));
                            //GameObject refer = (GameObject)reftile.GetValue(component);
                            //refer.renderer.material.color = new Color(1.0f, 0f, 0f, 0.4f);
                            //GameObject to = (GameObject)tileover.GetValue(component);
                            //to.renderer.material.color = new Color(1.0f, 0f, 0f, 0.4f);
                            
                            //component.getReference().renderer.material.color = new Color(1f, 0f, 0f, 0.4f);
                        }
                    }
                }*/
                
            }

           if (info.target is BattleMode && info.targetMethod.Equals("tileOver"))
            {
                showpicture = true;
                TilePosition tilepos = (TilePosition)info.arguments[0];
                this.currentHover.color = tilepos.color;
                this.currentHover.row = tilepos.row;
                this.currentHover.column = tilepos.column;
                //Unit unitFromTile = ((BattleMode)info.target).getUnitFromTile(component);
                Unit unitFromTile = ((BattleMode)info.target).getUnit(tilepos);
                Tile component = ((BattleMode)info.target).getTile(tilepos);
                enchlist.Clear();
                if (unitFromTile != null)
                {
                   List<EnchantmentInfo> enchants=  unitFromTile.getBuffs();
                    this.Allenchantcreator(enchants,component, false);
                }
            }
            if (info.target is BattleMode && info.targetMethod.Equals("tileOut"))
            {
                showpicture = false;
                this.currentHover.color = TileColor.unknown;
            }

            if (info.target is BattleMode && info.targetMethod.Equals("markMoveTile"))
            {
                TilePosition tilepos =(TilePosition)info.arguments[1];
                Tile tile = this.bm.getTile(tilepos);
                int bad = 0;
                foreach (TilePosition tp in this.enemyLobbers)
                {
                    if (tilepos.Equals(tp))
                    {
                        bad++;
                    }
                }

                if (bad>=1)
                {
                    //Console.WriteLine("bad spottet " +  tilepos);
                    GameObject to = (GameObject)tileover.GetValue(tile);
                    GameObject refer = (GameObject)reftile.GetValue(tile);
                    GameObject taf = (GameObject)targetAnimFront.GetValue(tile);
                    GameObject tab = (GameObject)targetAnimBack.GetValue(tile);
                    GameObject car = (GameObject)chargeAnim.GetValue(tile);
                    refer.renderer.material.color = new Color(1.0f, 0f, 0f, 0.2f * bad);
                    to.renderer.material.color = new Color(1.0f, 0f, 0f, 0.2f * bad);
                    if (taf != null) taf.renderer.material.color = new Color(1.0f, 0f, 0f, 0.2f * bad);
                    if (tab != null) tab.renderer.material.color = new Color(1.0f, 0f, 0f, 0.2f * bad);
                    if (car != null) car.renderer.material.color = new Color(1.0f, 0f, 0f, 0.2f * bad);
                }
            }


            if (this.mark && info.target is Tile && info.targetMethod.Equals("markInternal"))
            {

                TilePosition tpp = ((Tile)info.target).tilePosition();
                Tile tile = (Tile)info.target;
                int bad = 0;

               if (enemyLobbers.Count >= 1 && enemyLobbers[0].color == tpp.color && ((Tile.SelectionType)info.arguments[0]) == Tile.SelectionType.None)
                {
                    //this.mark = false;
                    return;
                }

                foreach (TilePosition tp in this.enemyLobbers)
                {
                    if (tpp.Equals(tp))
                    {
                        bad++;
                    }
                }

                if (bad >= 1)
                {
                    //Console.WriteLine("bad spottet " + tpp);
                    GameObject to = (GameObject)tileover.GetValue(tile);
                    GameObject refer = (GameObject)reftile.GetValue(tile);
                    GameObject taf = (GameObject)targetAnimFront.GetValue(tile);
                    GameObject tab = (GameObject)targetAnimBack.GetValue(tile);
                    refer.renderer.material.color = new Color(1.0f, 0f, 0f, 0.2f * bad);
                    to.renderer.material.color = new Color(1.0f, 0f, 0f, 0.2f * bad);
                    if (taf != null) taf.renderer.material.color = new Color(1.0f, 0f, 0f, 0.2f * bad);
                    if (tab != null) tab.renderer.material.color = new Color(1.0f, 0f, 0f, 0.2f * bad);
                }

            }

            return;
        }

        
	}
}

