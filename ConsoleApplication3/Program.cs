﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Collections.Concurrent;
using Bomberman.KozosKod;

namespace ConsoleApplication3
{
    class Program
    {
        static Random r = new Random();

        static Palya palya = new Palya(20, 20, 0.7);

        static uint Jatekos_ID_Szamlalo = 1;
        static Dictionary<uint, Jatekos> Jatekosok = new Dictionary<uint, Jatekos>();
        static uint Bomba_ID_Szamlalo = 1;
        static Dictionary<uint, Bomba> Bombak = new Dictionary<uint, Bomba>();
        static uint Lang_ID_Szamlalo = 1;
        static Dictionary<uint, Lang> Langok = new Dictionary<uint, Lang>();

        static void jatekos_pozicio_generalas()
        {
            uint x_db = (palya.Szelesseg - 2 - 1) / 2;
            uint y_db = (palya.Magassag - 2 - 1) / 2;

            for (int i = 0; i < Jatekosok.Count; i++)
            {
                while (true)
                {
                    uint x = (uint)(1 + r.Next((int)x_db) * 2);
                    uint y = (uint)(1 + r.Next((int)y_db) * 2);

                    bool talaltunke = false;

                    for (int j = 0; j < i; j++)
                    {
                        Jatekos jj = Jatekosok.Values.ElementAt(j);
                        if (jj.x == x && jj.y == y)
                        {
                            talaltunke = true;
                            break;
                        }
                    }

                    if (!talaltunke)
                    {
                        Jatekos jj = Jatekosok.Values.ElementAt(i);
                        jj.x = x;
                        jj.y = y;

                        palya.Cellak[x, y].Tipus = CellaTipus.Ures;
                        palya.Cellak[x + 1, y].Tipus = CellaTipus.Ures;
                        palya.Cellak[x, y + 1].Tipus = CellaTipus.Ures;
                        break;
                    }
                }

            }
        }

        static void bomba_telepites(uint jatekos_ID, uint bomba_x, uint bomba_y)
        {
            Jatekos j;
            if (!Jatekosok.TryGetValue(jatekos_ID, out j))
                return;

            if (j.Actbombaszam >= j.Maxbombaszam)
                return;

            if (!palya.uresE(bomba_x, bomba_y))
                return;

            Bomba b = new Bomba
            {
                ID = Bomba_ID_Szamlalo++,
                //Szin = j.Szin,
                Rendzs = j.Rendzs,
                Mikor_Robban = DateTime.Now.AddMilliseconds(3000),
                Jatekos_ID = j.ID,
                x = bomba_x,
                y = bomba_y
            };

            Bombak.Add(b.ID, b);

            palya.bomba_telepit(b);

            j.Actbombaszam++;
        }

        static void bomba_check()
        {
            foreach (Bomba b in Bombak.Values.ToList())
                if (b.Mikor_Robban <= DateTime.Now)
                    bomba_robban(b.ID);
        }

        static void bomba_robban(uint bomba_id)
        {
            Bomba b;
            if (!Bombak.TryGetValue(bomba_id, out b))
                return;

            Bombak.Remove(b.ID);

            Jatekos j;
            if (Jatekosok.TryGetValue(b.Jatekos_ID, out j))
            {
                if (j.Actbombaszam <= 0)
                    j.Actbombaszam = 0;

                else
                    j.Actbombaszam--;
            }

            palya.cellaTorol(b.x, b.y);

            lang_telepit(b.x, b.y, b);

            uint x = b.x;
            uint y = b.y;

            bool felmehete = true;
            bool jobbramehet = true;
            bool lemehet = true;
            bool balramehet = true;

            for (uint i = 1; i <= b.Rendzs; i++)
            {
                if (felmehete)
                    felmehete = lang_telepit(b.x, b.y - i, b);
                if (jobbramehet)
                    jobbramehet = lang_telepit(b.x + i, b.y, b);
                if (lemehet)
                    lemehet = lang_telepit(b.x, b.y + i, b);
                if (balramehet)
                    balramehet = lang_telepit(b.x - i, b.y, b);
            }
        }

        static bool lang_telepit(uint lang_x, uint lang_y, Bomba b)
        {
            if (lang_x >= palya.Szelesseg || lang_y >= palya.Magassag)
                return false;

            foreach (Jatekos j in Jatekosok.Values)
                if (j.x == lang_x && j.y == lang_y)
                {
                    csomiSzoras(new ChatCsomi(j.ID, "Jajj, meghaltam!"));
                    j.Ele = false;
                }

            switch (palya.Cellak[lang_x, lang_y].Tipus)
            {
                case CellaTipus.Ures:
                    {
                        Lang l = new Lang
                        {
                            ID = Lang_ID_Szamlalo++,
                            //Szin = b.Szin,
                            Meddig = DateTime.Now.AddMilliseconds(1000),
                            Jatekos_ID = b.Jatekos_ID,
                            x = lang_x,
                            y = lang_y
                        };

                        Langok.Add(l.ID, l);

                        palya.lang_telepit(l);

                        return true;
                    }
                case CellaTipus.Fal:
                    {
                        return false;
                    }
                case CellaTipus.Lang:
                    {
                        Langok.Remove(palya.Cellak[lang_x, lang_y].Lang_ID);

                        Lang l = new Lang
                        {
                            ID = Lang_ID_Szamlalo++,
                            //Szin = b.Szin,
                            Meddig = DateTime.Now.AddMilliseconds(1000),
                            Jatekos_ID = b.Jatekos_ID,
                            x = lang_x,
                            y = lang_y
                        };

                        Langok.Add(l.ID, l);

                        palya.lang_telepit(l);

                        return true;
                    }
                case CellaTipus.Bomba:
                    {
                        bomba_robban(palya.Cellak[lang_x, lang_y].Bomba_ID);
                        return false;
                    }
                case CellaTipus.Robbanthato_Fal:
                    {
                        palya.cellaTorol(lang_x, lang_y);
                        kartya_telepit(lang_x, lang_y, false);
                        return false;
                    }
                default:
                    {
                        palya.cellaTorol(lang_x, lang_y);
                        return false;
                    }
            }
        }

        static void lang_check()
        {
            foreach (Lang l in Langok.Values.ToList())
                if (l.Meddig < DateTime.Now)
                {
                    Langok.Remove(l.ID);
                    palya.cellaTorol(l.x, l.y);
                }
        }


        static void kartya_telepit(uint kartya_x, uint kartya_y, bool force)
        {
            if (kartya_x >= palya_szelesseg || kartya_y >= palya_magassag)
                return;

            if (Palya[kartya_x, kartya_y].Tipus != CellaTipus.Ures)
                return;

            // generáljunk kártyát?
            if (r.NextDouble() > 0.3 && !force)
                return;

            double valszeg = r.NextDouble();

            double also_hatar = 0;

            foreach (KartyaSuly ks in KartyaSulyok)
            {
                if (valszeg < (also_hatar + ks.Suly))
                {
                    Palya[kartya_x, kartya_y].Tipus = ks.KartyaTipus;
                    return;
                }
                else
                    also_hatar += ks.Suly;
            }
        }

        static Thread info;

        static void info_szal()
        {
            UdpClient c = new UdpClient();
            c.EnableBroadcast = true;

            IPEndPoint ep = new IPEndPoint(IPAddress.Broadcast, 60001);

            while (true)
            {
                using (MemoryStream ms = new MemoryStream())
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    bw.Write(szerverneve);
                    UInt16 tmp = (UInt16)((szerverjatekban) ? (1) : (0));
                    bw.Write(tmp);
                    tmp = (UInt16)Jatekosok.Count;
                    bw.Write(tmp);
                    c.Send(ms.ToArray(), (int)ms.Length, ep);
                }

                Thread.Sleep(500);
            }
        }

        static bool szerverjatekban = false;
        static String szerverneve = "";

        static void Main(string[] args)
        {
            szerverneve = Console.ReadLine();

            info = new Thread(new ThreadStart(info_szal));
            info.Start();

            TcpListener tl = new TcpListener(60000);
            tl.Start();

            palya_init(20, 20);

            while (true) // Csatalakozós ciklus
            {
                if (tl.Pending())
                {
                    Jatekos j = new Jatekos()
                    {
                        ID = Jatekos_ID_Szamlalo++,
                        Nev = "",
                        Rendzs = 1,
                        Maxbombaszam = 1,
                        Ele = true,
                        Sebesseg = 1,
                        CsomiSor = new ConcurrentQueue<String>(),
                        tcp = tl.AcceptTcpClient(),
                        thread = new Thread(new ParameterizedThreadStart(jatekos_szal))
                    };
                    j.CsomiSor.Enqueue("Üdv a világomban!");
                    Jatekosok.Add(j.ID, j);

                    j.thread.Start(j);
                }

                if (Console.KeyAvailable)
                    if (Console.ReadKey().KeyChar == 's')
                        break;
            }

            szerverjatekban = true;

            jatekos_pozicio_generalas();

            while (true)
            {
                bomba_check();
                lang_check();
                palya_kirajzol();
                System.Threading.Thread.Sleep(50);
            }
        }

        static bool jatekos_lep(uint uj_x, uint uj_y, Jatekos j)
        {
            if (
                uj_y < 0
                ||
                uj_x < 0
                ||
                uj_y >= palya_magassag
                ||
                uj_x >= palya_szelesseg
                )
                return false;

            lock (Palya)
            {
                switch (Palya[uj_x, uj_y].Tipus)
                {
                    case CellaTipus.Ures: break;
                    case CellaTipus.Fal: return false;
                    case CellaTipus.Robbanthato_Fal: return false;
                    case CellaTipus.Bomba: return false;
                    case CellaTipus.Lang:
                        j.Ele = false;
                        break;
                    case CellaTipus.Bomba_Kartya:
                        j.Maxbombaszam += 1;
                        Palya[uj_x, uj_y].Tipus = CellaTipus.Ures;
                        break;
                    case CellaTipus.Lang_Kartya:
                        j.Rendzs += 1;
                        Palya[uj_x, uj_y].Tipus = CellaTipus.Ures;
                        break;
                    case CellaTipus.Halalfej_Kartya: break;
                    case CellaTipus.Sebesseg_Kartya: break;
                    case CellaTipus.Lab_Kartya: break;
                    case CellaTipus.Kesztyu_Kartya: break;
                }
            }

            return true;
        }

        static void csomiSzoras(Csomi csomi)
        {
            foreach (Jatekos j in Jatekosok.Values.ToList())
                j.CsomiSor.Enqueue(csomi);
        }

        static void jatekos_szal(Object param)
        {
            Jatekos j = (Jatekos)param;

            try
            {
                using (BinaryWriter bw = new BinaryWriter(j.tcp.GetStream()))
                {
                    using (BinaryReader br = new BinaryReader(j.tcp.GetStream()))
                    {
                        bool Bemutatkozott = false;

                        while (true)
                        {
                            if (j.tcp.Available > 0)
                            {
                                int uzi_tipus = br.ReadByte();
                                switch ((Jatekos_Uzi_Tipusok)uzi_tipus)
                                {
                                    case Jatekos_Uzi_Tipusok.Bemutatkozik:
                                        Bemutatkozott = true;
                                        j.Nev = br.ReadString();
                                        UInt32 tmp = br.ReadUInt32();
                                        j.Arc = br.ReadBytes((int)tmp);

                                        csomiSzoras(new JatekosAdatokCsomi(j));
                                        break;
                                    case Jatekos_Uzi_Tipusok.Lep_Fel:
                                        if (!Bemutatkozott)
                                            break;

                                        if (!j.Ele)
                                            break;
                                        if (jatekos_lep(j.x, j.y - 1, j))
                                            j.y -= 1;
                                        break;
                                    case Jatekos_Uzi_Tipusok.Lep_Jobbra:
                                        if (!Bemutatkozott)
                                            break;
                                        if (!j.Ele)
                                            break;

                                        if (jatekos_lep(j.x + 1, j.y, j))
                                            j.x += 1;
                                        break;
                                    case Jatekos_Uzi_Tipusok.Lep_Le:
                                        if (!Bemutatkozott)
                                            break;

                                        if (!j.Ele)
                                            break;

                                        if (jatekos_lep(j.x, j.y + 1, j))
                                            j.y += 1;
                                        break;
                                    case Jatekos_Uzi_Tipusok.Lep_Balra:
                                        if (!Bemutatkozott)
                                            break;
                                        if (!j.Ele)
                                            break;
                                        if (jatekos_lep(j.x - 1, j.y, j))
                                            j.x -= 1;
                                        break;
                                    case Jatekos_Uzi_Tipusok.Bombat_rak:
                                        if (!Bemutatkozott)
                                            break;
                                        if (!j.Ele)
                                            break;
                                        bomba_telepites(j.ID, j.x, j.y);
                                        break;
                                    case Jatekos_Uzi_Tipusok.Chat:
                                        String uzike = br.ReadString();
                                        csomiSzoras(new ChatCsomi(j.ID, uzike));
                                        break;
                                }
                            }

                            j.CsomiSor.Enqueue(new JatekosokPoziciojaCsomi(Jatekosok.Values.ToList()));

                            Csomi tmp;

                            while (j.CsomiSor.TryDequeue(out tmp))
                                tmp.becsomagol(bw);





                            bw.Write((byte)Server_Uzi_Tipusok.Palyakep);

                            bw.Write(palya_szelesseg);
                            bw.Write(palya_magassag);

                            byte[] t = new byte[palya_szelesseg * palya_magassag];

                            for (int y = 0, tidx = 0; y < palya_magassag; y++)
                                for (int x = 0; x < palya_szelesseg; x++)
                                    t[tidx++] = (byte)Palya[x, y].Tipus;

                            bw.Write(t);
                            bw.Flush();

                            System.Threading.Thread.Sleep(25);
                        }
                    }
                }
            }
            catch
            {
                uzi_szoras(String.Format("{0}({1}):{2}", j.Nev, j.ID, "***KILLED***"));
            }
            finally
            {
                Jatekosok.Remove(j.ID);
                uzi_szoras(String.Format("{0}({1}):{2}", j.Nev, j.ID, "***CLOSED***"));
            }
        }
    }
}
