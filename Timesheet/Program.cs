using System;
using System.IO;
using System.Text.Json;
namespace CATerpillar.Program
{
    class Program
    {
        public ApplicationLayer app;
        public PresentationLayer ui;
        public PersistencyLayer file;
        public Menu mainMenu;
        public bool running = true;
        static void Main(string[] args)
        {
            Program p = new Program();
            p.Main();
        }

        public void Main()
        {
            this.app = new ApplicationLayer();
            this.ui = new PresentationLayer(app);
            this.file = new PersistencyLayer();
            BuildMenu();
            Menu currentMenu = mainMenu;
            while (running)
            {
                MenuEntry choice = Navigate(currentMenu);
                if(choice is Menu) { currentMenu = (Menu)choice; }
                if(choice is Option)
                {
                    Option pick = (Option)choice;
                    pick.Perform();
                }
            }
        }
        public void OptionStartWork(uiListable piece)
        {
           app.AddTicket(new Ticket((Piece)piece));            
        }
        public void OptionEndWork(uiListable listable)
        {
            Ticket ticket = (Ticket)listable;
            ticket.endWork();
        }
        public void OptionEndDay(uiListable session)
        {
            if (app.ticketExistsBy((t)=>!t.Closed))
            {
                Ticket[] open = app.FilterTicketsBy((t)=>!t.Closed);

                foreach (Ticket ticket in open)
                {
                    ticket.endWork();
                }
            }
            file.Save(app.GetPieces(), app.GetTickets(), (Session)session);
            running = false;
        }
        public void OptionCreatePiece()
        {
            app.AddPiece(ui.CreatePiece());
        }
        public void OptionEditPiece(uiListable piece)
        {
            ui.EditPiece((Piece)piece);   
        }
        public void OptionDeletePiece(uiListable piece)
        {
            app.DeletePiece(ui.DeletePiece((Piece)piece));   
        }
        public void OptionViewPieces()
        {
            ui.OutputListable(app.GetPieces());
        }
        public void OptionViewTickets()
        {    
            ui.OutputListable(app.GetTickets());            
        }
        public void OptionLoadSession(uiListable session)
        {
            app = file.Load((Session)session);
            ui = new PresentationLayer(app);
        }
        public void OptionSaveSession(uiListable session)
        {
            file.Save(app.GetPieces(), app.GetTickets(), (Session)session);
        }
        public bool FuncConditionPieces() { return app.piecesExist; }
        public bool FuncConditionTickets() { return app.ticketsExist; }
        public bool FuncConditionPiecesOrTickets() { return app.piecesExist || app.ticketsExist; }
        public bool FuncConditionSessions() { return file.GetFiles() != null; }
        public uiListable[] FuncGetAllPieces() { return app.GetPieces(); }
        public uiListable[] FuncGetAllTickets() { return app.GetTickets(); }
        public uiListable[] FuncGetAllSessions() { return file.GetFiles(); }
        public void BuildMenu()
        {
            mainMenu = new Menu(string.Format("{0}", "Welcome to CATerpillar, pick your poison:"));
            mainMenu.AddEntry(string.Format("{0}", "Start working on a piece"),
                              Piece.GetHeaderStatic(), FuncConditionPieces,FuncGetAllPieces, OptionStartWork, true);
            mainMenu.AddEntry(string.Format("{0}", "End working on a piece"), 
                              Ticket.GetHeaderStatic(), FuncConditionTickets,FuncGetAllTickets, OptionEndWork, true);
            Menu pieceMngmnt = mainMenu.AddEntry(string.Format("{0}", "Manage Pieces"), 
                                                 string.Format("Piece Manager (exit by inputing ..)"));
            Menu listMngmnt = mainMenu.AddEntry(string.Format("{0}", "See lists of your data"), 
                                                string.Format("List Viewer (exit by inputing ..)"),FuncConditionPiecesOrTickets);
            Menu fileMngmnt = mainMenu.AddEntry(string.Format("{0}", "Save/Load Session"), 
                                                string.Format("Session Manager (exit by inputing ..)"));
            mainMenu.AddEntry(string.Format("{0}", "End the day: End the work on all started pieces, save session to file and exit"),
                              Session.GetHeaderStatic(),FuncConditionSessions,FuncGetAllSessions, OptionEndDay,true);
            pieceMngmnt.AddEntry(string.Format("{0}", "Create a new piece"), OptionCreatePiece);
            pieceMngmnt.AddEntry(string.Format("{0}", "Edit an existing piece"), 
                                 Piece.GetHeaderStatic(),FuncConditionPieces,FuncGetAllPieces,OptionEditPiece, false);
            pieceMngmnt.AddEntry(string.Format("{0}", "Delete an existing piece (and all time-tickets for that piece)"),
                                 Piece.GetHeaderStatic(), FuncConditionPieces, FuncGetAllPieces, OptionDeletePiece,false);
            listMngmnt.AddEntry(string.Format("{0}", "See a list of all pieces"), OptionViewPieces, FuncConditionPieces);
            listMngmnt.AddEntry(string.Format("{0}", "See a list of all tickets"), OptionViewTickets, FuncConditionTickets);
            fileMngmnt.AddEntry(string.Format("{0}", "Load session from a file"), Session.GetHeaderStatic(), FuncConditionSessions, FuncGetAllSessions, OptionLoadSession, true);
            fileMngmnt.AddEntry(string.Format("{0}", "Save session to a file"), Session.GetHeaderStatic(), FuncConditionSessions, FuncGetAllSessions, OptionSaveSession, true);
        }
        public MenuEntry Navigate(Menu menu)
        {
            if (menu.Entries == null)
            {
                return menu.Parent;
            }
            OutputMenu(menu);
            Console.WriteLine(string.Format("{0}", "Pick an element from the list (exit by inputing ..)"));
            while (true)
            {
                string sAnswer = Console.ReadLine();
                int answer;
                if (sAnswer.Equals("..") && menu.Parent != null)
                {
                    return menu.Parent;
                }
                if (int.TryParse(sAnswer, out answer) && 0 <= answer && answer < menu.Entries.Length && menu.Entries[answer].isShown())
                {
                    return menu.Entries[answer];
                }
            }
        }
        public void OutputMenu(Menu menu)
        {
            Console.Clear();
            Console.WriteLine(menu.Heading);
            int i = 0;
            foreach (MenuEntry entry in menu.Entries)
            {
                if (entry.isShown())
                {
                    Console.WriteLine(string.Format("{0}\t{1}", i, entry.Description));
                }
                i++;
            }
        }
    }
    #region Menus and Entries
    abstract class MenuEntry
    {
        
        private string description;
        public string Description => description;
        protected MenuEntry(string d)
        {
            description = d;
        }
        abstract public bool isShown();
        
    }
    class Menu : MenuEntry
    {
        protected MenuEntry[] entries;
        private string heading;
        private Menu parent;
        public Menu Parent => parent;

        public virtual MenuEntry[] Entries => entries;
        public string Heading => heading;
        public Menu(string h): base(null)
        {
            heading = h;
            entries = new MenuEntry[0];
        }
        protected Menu(Menu p, string d, string h) : base(d)
        {
            parent = p;
            heading = h;
            entries = new MenuEntry[0];
        }
        public override bool isShown()
        {
            return true;
        }
        public Menu AddEntry(string d, string h)
        {
            Menu menu = new Menu(this, d, h);
            AddEntry(menu);
            return menu;
        }
        public virtual bool isAutoReturn()
        {
            return false;
        }
        public Menu AddEntry(string d, string h, Func<bool> c)
        {
            Menu menu = new ConditionalMenu(this, d, h, c);
            AddEntry(menu);
            return menu;
        }
        public void AddEntry(string d, string h, Func<bool> c, Func<uiListable[]> g, Action<uiListable> a, bool r)
        {
            AddEntry(new DynamicMenu(this, d, h, c, g, a, r));
        }
        public void AddEntry(string d, Action a)
        {
            AddEntry(new Option(d, a));
        }
        public void AddEntry(string d, Action a, Func<bool> c)
        {
            AddEntry(new ConditionalOption(d, a, c));
        }
        private void AddEntry(MenuEntry menuEntry)
        {
            MenuEntry[] newEntries = new MenuEntry[entries.Length + 1];
            int i = 0;
            foreach(MenuEntry oldEntry in entries)
            {
                newEntries[i] = oldEntry;
                i++;
            }
            newEntries[i] = menuEntry;
            entries = newEntries;
        }
        private void SetParent(Menu p)
        {
            parent = p;
        }
    }
    class ConditionalMenu: Menu
    {
        private Func<bool> condition;
        public ConditionalMenu(Menu p, string d, string h, Func<bool> c) : base(p, d, h)
        {
            condition = c;
        }
        public override bool isShown()
        {
            return condition();
        }
    }
    class DynamicMenu: ConditionalMenu
    {
        private Func<uiListable[]> getListables;
        private uiListable[] listables;
        private Action<uiListable> action;
        private bool autoReturn;
        public DynamicMenu(Menu p, string d, string h, Func<bool> c, Func<uiListable[]> g, Action<uiListable> a, bool r) : base(p, d, h, c)
        {
            getListables = g;
            action = a;
            autoReturn = r;
        }
        public override MenuEntry[] Entries
        {
            get
            {
                listables = getListables();
                if (listables == null)
                {
                    return null;
                }
                entries = new MenuEntry[listables.Length];
                int i = 0;
                foreach (uiListable listable in listables)
                {
                    entries[i] = new DynamicOption(listable.ToString(), action, GetListEntry);
                }
                return entries;
            }
        } 
        public uiListable GetListEntry(DynamicOption callingOption)
        {
            int i = 0;
            foreach (DynamicOption option in entries)
            {
                if (option == callingOption)
                {
                    return listables[i];
                }
                i++;
            }
            return null;
        }
        public override bool isAutoReturn()
        {
            return autoReturn;
        }
    }
    class Option: MenuEntry
    {
        private Action action;
        public Action Action => action;
        public Option(string d, Action a) : base(d)
        {
            action = a;
        }
        public override bool isShown()
        {
            return true;
        }
        public virtual void Perform()
        {
            action();
        }
    }
    class ConditionalOption : Option
    {
        private Func<bool> condition;
        public ConditionalOption(string d, Action a, Func<bool> c) : base(d, a)
        {
            condition = c;
        }
        public override bool isShown()
        {
            return condition();
        }
    }
    class DynamicOption : Option
    {
        private Func<DynamicOption, uiListable> getMyListEntry;
        private Action<uiListable> action;
        public DynamicOption(string d, Action<uiListable> a, Func<DynamicOption, uiListable> g) : base(d, null)
        {
            getMyListEntry = g;
            action = a;
        }
        public override void Perform()
        {
            action(getMyListEntry(this));
        }
    }
    #endregion
    interface uiListable
    {
        string GetHeader();
    }
    class Piece:uiListable
    {
        public string order { get; set; }
        public string project { get; set; }
        public string text { get; set; }
        
        public string GetHeader()
        {
            return Piece.GetHeaderStatic();
        }
        public static string GetHeaderStatic()
        {
            return string.Format("{0}\t{1}\t{2}", "project", "order", "text"); ;
        }
        public Piece(string o, string p, string t)
        {
            this.order = o;
            this.project = p;
            this.text = t;
        }
        public override string ToString()
        {
            return string.Format("{0}\t{1}\t{2}", project, order, text);
        }
    }
    class Ticket:uiListable
    {
        private Piece work;
        private DateTime start;
        private DateTime end;
        private string duration = "";
        private bool closed = false;

        public Piece Work { get { return this.work; } set { this.work = value; } }
        public DateTime Start { get { return this.start; } }
        public DateTime End { get { return this.end; } }
        public string Duration { get { return this.duration; } }
        public bool Closed { get { return this.closed; } }
        public Ticket(Piece p)
        {
            this.work = p;
            start = DateTime.Now;
        }
        
        public Ticket(Piece work, DateTime start, DateTime end, string duration, bool closed)
        {
            this.work = work;
            this.start = start;
            this.end = end;
            this.duration = duration;
            this.closed = closed;
        }
        public bool worksAt(Piece p)
        {
            return this.work == p;
        }
        public override string ToString()
        {
            string sEnd;
            if (!closed)
            {
                sEnd = "";
            }
            else
            {
                sEnd = string.Format("{0} {1}", end.ToShortDateString(), end.ToLongTimeString());
            }
            string sStart = string.Format("{0} {1}", start.ToShortDateString(), start.ToLongTimeString());
            return string.Format("{0}\t{1}\t{2}\t{3}", work.text, sStart, sEnd, duration);
        }
        public void CalculateDuration()
        {
            if (closed)
            {
                TimeSpan tsDuration = end - start;
                duration = string.Format("{0}", (int)tsDuration.TotalMinutes);
            }
        }
        public void endWork()
        {
            if (!closed)
            {
                closed = true;
                this.end = DateTime.Now;
                CalculateDuration();
            }
        }
        public string GetHeader()
        {
            return Ticket.GetHeaderStatic();
        }
        public static string GetHeaderStatic()
        {
            return string.Format("{0}\t{1}\t{2}\t{3}", "text", "start", "end", "duration");
        }
    }
    class Session:uiListable
    {
        private string name;
        public string Name { get { return this.name; } }
        public Session(string name)
        {
            this.name = name;
        }
        public string GetHeader()
        {
            return GetHeaderStatic();
        }
        public static string GetHeaderStatic()
        {
            return string.Format("{0}", "name");
        }
        public override string ToString()
        {
            return string.Format("{0}", name);
        }
    }
    class SessionContent
    {
        public Piece[] pieces;
        public Ticket[] tickets;
        public int[] indices;
        public SessionContent(Ticket[] t, Piece[] p)
        {
            pieces = p;
            if (t != null)
            {
                tickets = t;
                indices = new int[tickets.Length];
                int i = 0;
                foreach (Ticket ticket in tickets)
                {
                    int j = 0;
                    foreach (Piece piece in pieces)
                    {
                        if (ticket.Work == piece)
                        {
                            indices[i] = j;
                        }
                        j++;
                    }
                    i++;
                }
            }
            else
            {
                tickets = null;
                indices = null;
            }
        }
        public SessionContent(Ticket[] tickets, Piece[] pieces, int[] indices)
        {
            this.tickets = tickets;
            this.pieces = pieces;
            int i = 0;
            if (indices != null)
            {
                foreach (int index in indices)
                {
                    this.tickets[i].Work = this.pieces[index];
                }
            }
        }
    }
    #region Presentation
    class PresentationLayer
    {
        private ApplicationLayer app;
        public PresentationLayer(ApplicationLayer a)
        {
            this.app = a;
        }
        public void OutputListable(uiListable[] objects)
        {
            Console.WriteLine(string.Format("{0}\t{1}", "index", objects[0].GetHeader()));
            int i = 0;
            foreach (uiListable @object in objects)
            {
                Console.WriteLine(string.Format("{0}\t{1}", i, @object.ToString()));
                i++;
            }
        }
        public Piece CreatePiece()
        {
            Console.WriteLine(string.Format("{0}", "Project:"));
            string proj = Console.ReadLine();
            Console.WriteLine(string.Format("{0}", "Order:"));
            string ord = Console.ReadLine();
            Console.WriteLine(string.Format("{0}", "Text:"));
            string txt = Console.ReadLine();
            return new Piece(ord,proj,txt);
        }
        public Piece EditPiece(Piece piece)
        {
            string proj = EditField("Project:", piece.project);
            string ord  = EditField("Order:", piece.order);
            string txt = EditField("Text:", piece.text);
            piece.project = proj;
            piece.order = ord;
            piece.text = txt;
            return piece;
        }
        public Piece DeletePiece(Piece piece)
        {
            if (app.ticketExistsBy((t) => t.worksAt(piece))) 
            {
                Console.WriteLine(string.Format("{0}", "There are already tickets for this piece:"));
                Ticket[] tickets = app.FilterTicketsBy((t) => t.worksAt(piece));
                OutputListable(tickets);
                if(Confirm(string.Format("{0}", "Delete piece and tickets?")))
                {
                    app.DeleteTickets(tickets);
                    Console.WriteLine(string.Format("{0}", "Tickets Deleted"));
                    return DeletePiece(piece);
                }
                else
                {
                    return null;
                }
            }
            else
            {
                if(Confirm(string.Format("Delete: {0}?", piece.text))){
                    return piece;
                }
                else
                {
                    return null;
                }
            }
        }
        public bool Confirm(string prompt)
        {
            Console.WriteLine(string.Format("{0} Y(es)/N(o):", prompt));
            while (true)
            {
                string answer = Console.ReadLine();
                if (answer.Equals("Y")){ return true; }
                if (answer.Equals("N")){ return false; }
            }
        }
        public string EditField(string prompt, string oldValue)
        {
            Console.WriteLine(string.Format("{0}", prompt));
            Console.WriteLine(string.Format("({0})", oldValue));
            string newValue = Console.ReadLine();
            if (newValue.Equals(""))
            {
                if (Confirm(string.Format("{0}: {1}", "Keep", oldValue)))
                {
                    return oldValue;
                }
                else
                {
                    return "";
                }
            }
            return newValue;
        }
    }
    #endregion //Presentation
    #region Persistency
    class PersistencyLayer
    {
        public Session[] GetFiles()
        {
            string[] paths = Directory.GetFiles(".\\saves", "*.json");
            Session[] sessions = new Session[paths.Length];
            int i = 0;
            foreach (string path in paths)
            {
                sessions[i] = new Session(Path.GetFileNameWithoutExtension(path));
                i++;
            }
            return sessions;
        }
        public void Save(Piece[] pieces, Ticket[] tickets, Session session)
        {
            SessionContent content = new SessionContent(tickets, pieces);
            string jcontent = JsonSerializer.Serialize(content);
            File.WriteAllText(string.Format(".\\saves\\{0}.json", session.Name), jcontent);
        }
        public ApplicationLayer Load(Session session)
        {
            string jcontent = File.ReadAllText(string.Format(".\\saves\\{0}.json", session.Name));
            SessionContent content = JsonSerializer.Deserialize<SessionContent>(jcontent);
            ApplicationLayer app = new ApplicationLayer(content.tickets, content.pieces);
            Ticket[] closed = app.FilterTicketsBy((t) => t.Closed);
            if (closed != null)
            {
                foreach(Ticket ticket in closed)
                {
                    ticket.CalculateDuration(); //recalculate Durations
                }
            }
            return app;
        }
    }
    #endregion //Persistency
    #region Application
    class ApplicationLayer
    {
        private Ticket[] tickets;
        private Piece[] pieces;
        public bool ticketsExist;
        public bool piecesExist;
        public ApplicationLayer() { }
        public ApplicationLayer(Ticket[] tickets, Piece[] pieces)
        {
            this.tickets = tickets;
            this.pieces = pieces;
        }
        public Ticket[] GetTickets()
        {
            return tickets;
        }
        public Piece[] GetPieces()
        {
            return pieces;
        }
        public void AddTicket(Ticket t)
        {
            if (tickets == null)
            {
                tickets = new Ticket[1];
                tickets[0] = t;
            }
            else
            {
                Ticket[] newTickets = new Ticket[this.tickets.Length + 1];
                int i = 0;
                foreach (Ticket ticket in this.tickets)
                {
                    newTickets[i] = ticket;
                    i++;
                }
                newTickets[i] = t;
                this.tickets = newTickets;
            }
            this.ticketsExist = true;
        }
        public void DeleteTickets(Ticket[] tickets)
        {
            if (this.tickets.Length - tickets.Length == 0)
            {
                this.tickets = null;
                ticketsExist = false;
            }
            else
            {
                Ticket[] newTickets = new Ticket[this.tickets.Length - tickets.Length];
                bool del;
                int i = 0;
                foreach (Ticket oldTicket in tickets)
                {
                    del = false;
                    foreach (Ticket badTicket in tickets)
                    {
                        if (oldTicket == badTicket) del = true;
                    }
                    if (!del)
                    {
                        newTickets[i] = oldTicket;
                        i++;
                    }
                }
                this.tickets = newTickets;
            }
        }
        public void AddPiece(Piece p)
        {
            if (this.pieces == null)
            {
                this.pieces = new Piece[1];
                this.pieces[0] = p;
            }
            else
            {
                Piece[] newPieces = new Piece[this.pieces.Length + 1];
                int i = 0;
                foreach (Piece piece in this.pieces)
                {
                    newPieces[i] = piece;
                    i++;
                }
                newPieces[i] = p;
                this.pieces = newPieces;
            }
            piecesExist = true;
        }
        public void DeletePiece(Piece p)
        {
            if (pieces.Length == 1)
            {
                pieces = null;
                piecesExist = false;
            }
            else
            {
                Piece[] newPieces = new Piece[pieces.Length - 1];
                int i = 0;
                foreach (Piece oldPiece in pieces)
                {
                    if (oldPiece != p)
                    {
                        newPieces[i] = oldPiece;
                        i++;
                    }
                }
                pieces = newPieces;
            }
        }
        public bool ticketExistsBy(Func<Ticket, bool> condition)
        {
            if (tickets == null) return false;
            foreach (Ticket ticket in tickets)
            {
                if (condition(ticket)) { return true; }
            }
            return false;
        }
        public int countTicketsBy(Func<Ticket, bool> condition)
        {
            int i = 0;
            foreach (Ticket ticket in tickets)
            {
                if (condition(ticket)) { i++; }
            }
            return i;
        }
        public Ticket[] FilterTicketsBy(Func<Ticket, bool> condition )
        {
            Ticket[] filteredTickets = new Ticket[countTicketsBy(condition)];
            int i = 0;
            foreach (Ticket ticket in tickets)
            {
                if (condition(ticket)) { filteredTickets[i] = ticket; }
            }
            return filteredTickets;
        }


    }
    #endregion //Application
}
