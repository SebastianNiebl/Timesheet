using System;
using System.Collections.Generic;

namespace CATerpillar.Menu
{
    class MenuEntry
    {
        private MenuEntry parent;
        private List<MenuEntry> children;
        private Action staticPerform;
        private string description;
        private string header;
        private Action<uiListable> dynamicPerformPrePick;
        private Action<uiListable> dynamicPerformPostPick;
        private Func<List<uiListable>> getDynamicEntries;
        private bool isDynamic;
        private bool isDummy;
        private Func<bool> isVisible;
        private Func<bool> endsProgram;
        private MenuEntry dummy;

        protected MenuEntry()
        {
            parent = null;
            children = new List<MenuEntry>();
            staticPerform = DefaultMenuMethods.NoOp;
            dynamicPerformPrePick = DefaultMenuMethods.NoOp;
            dynamicPerformPostPick = DefaultMenuMethods.NoOp;
            getDynamicEntries = DefaultMenuMethods.Empty;
            isDynamic = false;
            isDummy = false;
            isVisible = DefaultMenuMethods.Yes;
            endsProgram = DefaultMenuMethods.No;
            description = "";
            header = "";
            dummy = null;
        }
        public MenuEntry GetParent() { return parent; }
        public List<MenuEntry> GetChildren() { return children; }
        public void StaticPerform() { staticPerform(); }
        public Action<uiListable> GetDynamicPerformPrePick() { return dynamicPerformPrePick; } //A better name would be dynamicPerformConext
        public Action<uiListable> GetDynamicPerformPostPick() { return dynamicPerformPostPick; } //A better name would be dynamicPerformChoice
        public List<uiListable> GetDynamicEntries() { return getDynamicEntries(); }
        public bool IsVisible() { return isVisible(); }
        public bool EndsProgram() { return endsProgram(); }
        public bool IsDynamic() { return isDynamic; }
        public bool IsDummy() { return isDummy; }
        public string GetDescription() { return description; }
        public string GetHeader() { return header; }
        protected void SetParent(MenuEntry p) { parent = p; }
        protected void AddChild(MenuEntry c) { children.Add(c); }
        protected void SetStaticPerform(Action a) { staticPerform = a; }
        protected void SetDynamicPerformPrePick(Action<uiListable> a) { dynamicPerformPrePick = a; }
        protected void SetDynamicPerformPostPick(Action<uiListable> a) { dynamicPerformPostPick = a; }
        protected void SetFuncGetDynamicEntries(Func<List<uiListable>> f) { getDynamicEntries = f; }
        protected void SetFuncIsVisible(Func<bool> f) { isVisible = f; }
        protected void SetFuncEndsProgram(Func<bool> f) { endsProgram = f; }
        protected void SetDescription(string d) { description = d; }
        protected void SetHeader(string h) { header = h; }
        protected void RemoveDummy()
        {
            if (children[0].IsDummy())
            {
                dummy = children[0];
                children.Remove(children[0]);
            }
        }
        protected void ReplaceAllChildrenWithDummy()
        {
            children.Clear();
            children.Add(dummy);
        }
        public class Builder
        {
            MenuEntry root;
            MenuEntry current;

            public Builder()
            {
                root = new MenuEntry();
                current = root;
            }
            public Builder SetStaticPerform(Action a)
            {
                current.SetStaticPerform(a);
                return this;
            }
            public Builder SetDynamicPerformPrePick(Action<uiListable> a)
            {
                current.SetDynamicPerformPrePick(a);
                return this;
            }
            public Builder SetDynamicPerformPostPick(Action<uiListable> a)
            {
                current.SetDynamicPerformPostPick(a);
                return this;
            }
            public Builder SetFuncGetDynamicEntries(Func<List<uiListable>> f)
            {
                current.SetFuncGetDynamicEntries(f);
                if (f != DefaultMenuMethods.Empty)
                {
                    current.isDynamic = true;
                }
                return this;
            }
            public Builder SetFuncIsVisible(Func<bool> f)
            {
                current.SetFuncIsVisible(f);
                return this;
            }
            public Builder SetFuncEndsProgram(Func<bool> f)
            {
                current.SetFuncEndsProgram(f);
                return this;
            }
            public Builder AddChild()
            {
                MenuEntry temp = new MenuEntry();
                temp.SetParent(current);
                if (current.IsDynamic())
                {
                    temp.isDummy = true;
                }
                current.AddChild(temp);
                current = temp;
                return this;
            }
            
            public Builder Return()
            {
                if(current.GetParent() != null)
                current = current.GetParent();
                return this;
            }
            public Builder SetHeader(string h)
            {
                current.SetHeader(h);
                return this;
            }
            public Builder SetDescription(string d)
            {
                current.SetDescription(d);
                return this;
            }
            internal Builder SetParent(MenuEntry p)
            {
                current.SetParent(p);
                return this;
            }
            internal Builder AddChild(MenuEntry m)
            {
                current.AddChild(m);
                m.SetParent(current);
                current = m;
                return this;
            }
            public MenuEntry Build()
            {
                return root;
            }
        }
        class Navigator
        {
            private MenuEntry root;
            private MenuEntry current;
            private uiListable context;

            public Navigator(MenuEntry r)
            {
                root = r;
                current = root;
            }
            public bool Navigate()
            {
                int chosenNumber = 0;
                if (current.IsDynamic())
                {
                    MenuEntry dummy = current.GetChildren()[0];
                    current.RemoveDummy();
                    foreach (uiListable listable in current.GetDynamicEntries())
                    {
                        Builder b = new Builder();
                        b = b.SetHeader(dummy.GetHeader()).
                                           SetDescription(listable.GetDescription()).
                                           SetStaticPerform(dummy.staticPerform).
                                           SetDynamicPerformPrePick(dummy.dynamicPerformPrePick).
                                           SetDynamicPerformPostPick(dummy.dynamicPerformPostPick).
                                           SetFuncGetDynamicEntries(dummy.getDynamicEntries).
                                           SetFuncIsVisible(dummy.isVisible).
                                           SetFuncEndsProgram(dummy.endsProgram).
                                           SetParent(current);
                        foreach(MenuEntry child in dummy.GetChildren())
                        {
                            b = b.AddChild(child).Return();
                        }
                        current.AddChild(b.Build());
                    }
                }
                MenuEntry choice = LetUserPick(current, ref chosenNumber);
                if (chosenNumber == -1)
                {
                    if (current.IsDynamic())
                    {
                        if (current.GetDynamicEntries().Count == 0 && choice == null) { return false; }
current.ReplaceAllChildrenWithDummy();
                    }
                    if(choice == null) { return true; }
                }
                else
                {
                    choice.StaticPerform();
                    if (current.IsDynamic())
                    {
                        current.GetDynamicPerformPostPick()(current.GetDynamicEntries()[chosenNumber]);
                    }
                    if (context != null)
                    {
                        choice.GetDynamicPerformPrePick()(context);
                    }
                    if (current.IsDynamic())
                    {
                        context = current.GetDynamicEntries()[chosenNumber];
                    }
                    else
                    {
                        context = null;
                    }
                }
                current = choice;
                return current.EndsProgram();
            }
            private MenuEntry LetUserPick(MenuEntry menu, ref int choice)
            {
                if (current.GetChildren().Count == 0)
                {
                    choice = -1;
                    return current.GetParent();
                }
                OutputMenu(menu);
                Console.WriteLine(string.Format("{0}", "Pick an element from the list"));
                while (true)
                {
                    string sAnswer = Console.ReadLine();
                    int answer;
                    if (sAnswer.Equals("..") && menu.GetParent() != null)
                    {
                        choice = -1;
                        return menu.GetParent();
                    }
                    if (int.TryParse(sAnswer, out answer) && 0 <= answer && answer < menu.GetChildren().Count && menu.GetChildren()[answer].IsVisible())
                    {
                        choice = answer;
                        return menu.GetChildren()[answer];
                    }
                }
            }
            private void OutputMenu(MenuEntry menu)
            {
                Console.Clear();
                Console.WriteLine(menu.GetHeader());
                int i = 0;
                foreach (MenuEntry entry in menu.GetChildren())
                {
                    if (entry.IsVisible())
                    {
                        Console.WriteLine(string.Format("{0}\t{1}", i, entry.GetDescription()));
                    }
                    i++;
                }
            }
        }
    }
    static class DefaultMenuMethods
    {
        public static void NoOp() { }
        public static void NoOp(uiListable ignored) { }
        public static List<uiListable> Empty() { return new List<uiListable>(); }
        public static bool Yes() { return true; }
        public static bool No() { return false; }
    }
    interface uiListable
    {
        string GetHeader();
        string GetDescription();
    }
}
