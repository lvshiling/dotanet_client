﻿using cocosocket4unity;
using FairyGUI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Google.Protobuf;
using System.Threading;
using System;

public class GuildInfo
{
    private GComponent main;
    
    public GuildInfo()
    {
        if (GameScene.Singleton.m_MyMainUnit == null)
        {
            return;
        }

        MsgManager.Instance.AddListener("SC_GetAllGuildsInfo", new HandleMsg(this.SC_GetAllGuildsInfo));
        MsgManager.Instance.AddListener("SC_GetGuildInfo", new HandleMsg(this.SC_GetGuildInfo));
        MsgManager.Instance.AddListener("SC_GetJoinGuildPlayer", new HandleMsg(this.SC_GetJoinGuildPlayer));
        

        if (GameScene.Singleton.m_MyMainUnit.GuildID > 0)
        {
            //自己有公会
            //获取数据
            Protomsg.CS_GetGuildInfo msg1 = new Protomsg.CS_GetGuildInfo();
            msg1.ID = GameScene.Singleton.m_MyMainUnit.GuildID;
            MyKcp.Instance.SendMsg(GameScene.Singleton.m_ServerName, "CS_GetGuildInfo", msg1);
            AudioManager.Am.Play2DSound(AudioManager.Sound_OpenUI);
        }
        else
        {
            //自己无公会
            Protomsg.CS_GetAllGuildsInfo msg1 = new Protomsg.CS_GetAllGuildsInfo();
            MyKcp.Instance.SendMsg(GameScene.Singleton.m_ServerName, "CS_GetAllGuildsInfo", msg1);
            AudioManager.Am.Play2DSound(AudioManager.Sound_OpenUI);

        }




    }
    //弹出创建公会对话框
    public void createguildwindow(int price,int pricetype)
    {
        var createguildwd = UIPackage.CreateObject("GameUI", "CreateGuild").asCom;
        GRoot.inst.AddChild(createguildwd);
        createguildwd.xy = Tool.GetPosition(0.5f, 0.5f);
        createguildwd.GetChild("close").asButton.onClick.Add(() =>
        {
            createguildwd.Dispose();
        });

        createguildwd.GetChild("create").asButton.onClick.Add(() =>
        {
            var txt = createguildwd.GetChild("input").asTextInput.text;
            if (txt.Length <= 0)
            {
                Tool.NoticeWords("请输入名字！", null);
                return;
            }
            if (Tool.IsChineseOrNumberOrWord(txt) == false)
            {
                Tool.NoticeWords("名字不能含有中文,字母,数字以外的其他字符！", null);
                return;
            }
            //创建 
            Protomsg.CS_CreateGuild msg1 = new Protomsg.CS_CreateGuild();
            msg1.Name = txt;
            MyKcp.Instance.SendMsg(GameScene.Singleton.m_ServerName, "CS_CreateGuild", msg1);
            createguildwd.Dispose();
        });

        createguildwd.GetChild("price").asTextField.text = price + "";
        createguildwd.GetChild("pricetype").asLoader.url = Tool.GetPriceTypeIcon(pricetype);
    }


    public bool SC_GetAllGuildsInfo(Protomsg.MsgBase d1)
    {
        Debug.Log("SC_GetAllGuildsInfo:");
        IMessage IMperson = new Protomsg.SC_GetAllGuildsInfo();
        Protomsg.SC_GetAllGuildsInfo p1 = (Protomsg.SC_GetAllGuildsInfo)IMperson.Descriptor.Parser.ParseFrom(d1.Datas);

        //创建界面
        if (main != null)
        {
            main.Dispose();
        }
        main = UIPackage.CreateObject("GameUI", "AllGuilds").asCom;
        GRoot.inst.AddChild(main);
        main.xy = Tool.GetPosition(0.5f, 0.5f);
        main.GetChild("close").asButton.onClick.Add(() =>
        {
            this.Destroy();
        });
        //创建公会按钮
        main.GetChild("add").asButton.onClick.Add(() =>
        {
            this.createguildwindow(p1.CreatePrice, p1.CreatePriceType);
        });
        
        //main.GetChild("list").asList.RemoveChildren(0, -1, true);
        //处理排序
        Protomsg.GuildShortInfo[] allplayer = new Protomsg.GuildShortInfo[p1.Guilds.Count];
        int index = 0;
        foreach (var item in p1.Guilds)
        {
            allplayer[index++] = item;
        }
        System.Array.Sort(allplayer, (a, b) => {

            if(a.Level > b.Level)
            {
                return 1;
            }
            else if(a.Level == b.Level)
            {
                if( a.Experience > b.Experience)
                {
                    return 1;
                }
                else
                {
                    return -1;
                }
                
            }
            else
            {
                return -1;
            }
        });
        foreach (var item in allplayer)
        {
            var onedropitem = UIPackage.CreateObject("GameUI", "GuildsOne").asCom;

            onedropitem.GetChild("add").onClick.Add(() =>
            {
                //申请加入公会
                Protomsg.CS_JoinGuild msg1 = new Protomsg.CS_JoinGuild();
                msg1.ID = item.ID;
                MyKcp.Instance.SendMsg(GameScene.Singleton.m_ServerName, "CS_JoinGuild", msg1);
            });
            onedropitem.GetChild("name").asTextField.text = item.Name;
            onedropitem.GetChild("level").asTextField.text = item.Level+"";
            onedropitem.GetChild("count").asTextField.text = item.CharacterCount + "/"+item.MaxCount;
            onedropitem.GetChild("president").asTextField.text = item.PresidentName;
            onedropitem.GetChild("levellimit").asTextField.text = item.Joinlevellimit + "";

            main.GetChild("list").asList.AddChild(onedropitem);
        }

        return true;
    }
    public bool SC_GetGuildInfo(Protomsg.MsgBase d1)
    {
        Debug.Log("SC_GetGuildInfo:");
        IMessage IMperson = new Protomsg.SC_GetGuildInfo();
        Protomsg.SC_GetGuildInfo p1 = (Protomsg.SC_GetGuildInfo)IMperson.Descriptor.Parser.ParseFrom(d1.Datas);
        //创建界面
        if (main != null)
        {
            main.Dispose();
        }
        main = UIPackage.CreateObject("GameUI", "GuildInfo").asCom;
        GRoot.inst.AddChild(main);
        main.xy = Tool.GetPosition(0.5f, 0.5f);
        main.GetChild("close").asButton.onClick.Add(() =>
        {
            this.Destroy();
        });
        //
        main.GetChild("request").asButton.onClick.Add(() =>
        {
            //查看申请列表
            Protomsg.CS_GetJoinGuildPlayer msg1 = new Protomsg.CS_GetJoinGuildPlayer();
            msg1.ID = p1.GuildBaseInfo.ID;
            MyKcp.Instance.SendMsg(GameScene.Singleton.m_ServerName, "CS_GetJoinGuildPlayer", msg1);
        });

        //-------------------------公会成员--------------------------
        //处理排序
        Protomsg.GuildChaInfo[] allplayer = new Protomsg.GuildChaInfo[p1.Characters.Count];
        int index = 0;
        foreach (var item in p1.Characters)
        {
            allplayer[index++] = item;
        }
        System.Array.Sort(allplayer, (a, b) => {

            if (a.Level > b.Level)
            {
                return 1;
            }
            else if (a.Level == b.Level)
            {
                if (a.PinLevel > b.PinLevel)
                {
                    return 1;
                }
                else if (a.PinLevel == b.PinLevel)
                {
                    if(a.PinExperience > b.PinExperience)
                    {
                        return 1;
                    }
                    else
                    {
                        return -1;
                    }
                }
                else
                {
                    return -1;
                }

            }
            else
            {
                return -1;
            }
        });
        foreach (var item in allplayer)
        {
            var onedropitem = UIPackage.CreateObject("GameUI", "GuildPlayerOne").asCom;

            onedropitem.GetChild("add").onClick.Add(() =>
            {
                //踢出公会
                Protomsg.CS_DeleteGuildPlayer msg1 = new Protomsg.CS_DeleteGuildPlayer();
                msg1.Characterid = item.Characterid;
                MyKcp.Instance.SendMsg(GameScene.Singleton.m_ServerName, "CS_DeleteGuildPlayer", msg1);
            });
            onedropitem.GetChild("name").asTextField.text = item.Name;
            onedropitem.GetChild("level").asTextField.text = item.Level + "";
            onedropitem.GetChild("pinlevel").asTextField.text = Tool.GuildPinLevelWords[item.PinLevel];
            onedropitem.GetChild("post").asTextField.text = Tool.GuildPostWords[item.Post];

            var clientitem = ExcelManager.Instance.GetUnitInfoManager().GetUnitInfoByID(item.Typeid);
            if (clientitem != null)
            {
                onedropitem.GetChild("heroicon").asLoader.url = clientitem.IconPath;
            }

            main.GetChild("mainlist").asList.AddChild(onedropitem);
        }
        //-----------------公会信息------------------
        main.GetChild("name").asTextField.text = p1.GuildBaseInfo.Name;
        main.GetChild("level").asTextField.text = "Lv."+p1.GuildBaseInfo.Level;
        main.GetChild("experience").asTextField.text = p1.GuildBaseInfo.Experience+"/"+p1.GuildBaseInfo.MaxExperience;
        main.GetChild("playercount").asTextField.text = p1.GuildBaseInfo.CharacterCount+"/"+p1.GuildBaseInfo.MaxCount;
        main.GetChild("gonggao").asTextField.text = p1.GuildBaseInfo.Notice;
        return true;
    }
    public bool SC_GetJoinGuildPlayer(Protomsg.MsgBase d1)
    {
        Debug.Log("SC_GetJoinGuildPlayer:");
        IMessage IMperson = new Protomsg.SC_GetJoinGuildPlayer();
        Protomsg.SC_GetJoinGuildPlayer p1 = (Protomsg.SC_GetJoinGuildPlayer)IMperson.Descriptor.Parser.ParseFrom(d1.Datas);
        //创建界面
        if (main == null)
        {
            return true;
        }

        //-------------------------公会成员--------------------------
        main.GetChild("requestlist").asList.RemoveChildren(0, -1, true);
        //处理排序
        Protomsg.GuildChaInfo[] allplayer = new Protomsg.GuildChaInfo[p1.RequestCharacters.Count];
        int index = 0;
        foreach (var item in p1.RequestCharacters)
        {
            allplayer[index++] = item;
        }
        System.Array.Sort(allplayer, (a, b) => {

            if (a.Level > b.Level)
            {
                return 1;
            }
            else
            {
                return -1;
            }
        });
        foreach (var item in allplayer)
        {
            var onedropitem = UIPackage.CreateObject("GameUI", "GuildRequestPlayerOne").asCom;

            onedropitem.GetChild("agree").onClick.Add(() =>
            {
                //同意
                Protomsg.CS_ResponseJoinGuildPlayer msg1 = new Protomsg.CS_ResponseJoinGuildPlayer();
                msg1.Characterid = item.Characterid;
                msg1.Result = 1;
                MyKcp.Instance.SendMsg(GameScene.Singleton.m_ServerName, "CS_ResponseJoinGuildPlayer", msg1);
            });
            onedropitem.GetChild("no").onClick.Add(() =>
            {
                //拒绝
                Protomsg.CS_ResponseJoinGuildPlayer msg1 = new Protomsg.CS_ResponseJoinGuildPlayer();
                msg1.Characterid = item.Characterid;
                msg1.Result = 0;
                MyKcp.Instance.SendMsg(GameScene.Singleton.m_ServerName, "CS_ResponseJoinGuildPlayer", msg1);
            });
            onedropitem.GetChild("name").asTextField.text = item.Name;
            onedropitem.GetChild("level").asTextField.text = item.Level + "";
            

            var clientitem = ExcelManager.Instance.GetUnitInfoManager().GetUnitInfoByID(item.Typeid);
            if (clientitem != null)
            {
                onedropitem.GetChild("heroicon").asLoader.url = clientitem.IconPath;
            }

            main.GetChild("requestlist").asList.AddChild(onedropitem);
        }
       
        return true;
    }
    

    //
    public void Destroy()
    {
        MsgManager.Instance.RemoveListener("SC_GetAllGuildsInfo");
        MsgManager.Instance.RemoveListener("SC_GetGuildInfo");
        MsgManager.Instance.RemoveListener("SC_GetJoinGuildPlayer");
        AudioManager.Am.Play2DSound(AudioManager.Sound_CloseUI);
        if (main != null)
        {
            main.Dispose();
        }
    }

    
}
