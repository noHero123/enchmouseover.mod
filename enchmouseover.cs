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

    /*struct playerinfo 
    {
        public string ID;
        public string name;
    }*/

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

        private MethodInfo mi;
        private FieldInfo reftile;
        private FieldInfo tileover;
        private FieldInfo mrktpe;
        private FieldInfo sbfrm;
        private int cardnametoimageid(string name) { return cardImageid[Array.FindIndex(cardnames, element => element.Equals(name))]; }

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

                App.Communicator.removeListener(this);//dont need the listener anymore
            }

            return;
        }
        public void onReconnect()
        {
            return; // don't care
        }


        public void writetxtinchat(string msgs)
        {

            Console.WriteLine(msgs);
            try
            {
                String chatMsg = msgs;
                MethodInfo mi = typeof(BattleMode).GetMethod("updateChat", BindingFlags.NonPublic | BindingFlags.Instance);
                if (mi != null) // send chat message
                {
                    mi.Invoke(this.bm, new String[] { chatMsg });
                }
                else // can't invoke updateChat
                {
                }
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
			return 5;
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
                        if (enchname == "Poison") { enchname = "Ranger's Bane"; };
                        if (enchname == "Curse 1") { enchname = "Cluster Hex"; };
                        if (enchname == "Curse 2") { enchname = "Cluster Hex"; };
                        if (enchname == "Decomposing") { enchname = "Return To Nature"; };
                        Texture2D newtexture = new Texture2D(2, 2, TextureFormat.RGB24, false);
                        newtexture = App.AssetLoader.LoadTexture2D(cardnametoimageid(enchname).ToString());
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
                   scrollsTypes["BattleMode"].Methods.GetMethod("tileOver", new Type[]{typeof(Tile),typeof(int),typeof(int)} ),
                    scrollsTypes["BattleMode"].Methods.GetMethod("tileOut", new Type[]{typeof(GameObject),typeof(int),typeof(int)} ),
                    scrollsTypes["BattleMode"].Methods.GetMethod("toggleUnitStats")[0],
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

        public override void BeforeInvoke(InvocationInfo info)
        {


            return;

        }

        public override void AfterInvoke (InvocationInfo info, ref object returnValue)
        
        {
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
                        Tile component = ((BattleMode)info.target).getTileFromUnit(current);
                        this.Allenchantcreator(enchants, component, true);
              
                    }

                }
            }


            if (info.target is BattleMode && info.targetMethod.Equals("OnGUI"))
            {
                if (bm == null)
                {
                    bm = (BattleMode)info.target;
                }


                


                if (showall == true && this.allwayson==true)
                {

                    Allenchlist.Clear();
                    
                    foreach (Unit current in (List<Unit>)mi.Invoke(this.bm, null))
                    {
                        List<EnchantmentInfo> enchants = current.getBuffs();
                        Tile component = ((BattleMode)info.target).getTileFromUnit(current);
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

                
                foreach (Unit current in (List<Unit>)mi.Invoke(this.bm, null))
                {
                    current.renderer.material.color = new Color(1f, 1f, 1f, 1f);
                    List<EnchantmentInfo> enchants = current.getBuffs();
                    Tile component = ((BattleMode)info.target).getTileFromUnit(current);
                    if (enchants.Count > 0)
                    {
                        Tile.SelectionType marker = (Tile.SelectionType)mrktpe.GetValue(component);
                        if (marker != Tile.SelectionType.Hover)
                        {
                            current.renderer.material.color = new Color(1.0f, 0.5f, 0.5f, 0.7f);
                            //GameObject refer = (GameObject)reftile.GetValue(component);
                            //refer.renderer.material.color = new Color(1.0f, 0f, 0f, 0.4f);
                            //GameObject to = (GameObject)tileover.GetValue(component);
                            //to.renderer.material.color = new Color(1.0f, 0f, 0f, 0.4f);
                        }
                    }
                }
                
            }

           if (info.target is BattleMode && info.targetMethod.Equals("tileOver"))
            {
                showpicture = true;
                Tile component = (Tile)info.arguments[0];
                Unit unitFromTile = ((BattleMode)info.target).getUnitFromTile(component);
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
            }





            return;
        }

        
	}
}

