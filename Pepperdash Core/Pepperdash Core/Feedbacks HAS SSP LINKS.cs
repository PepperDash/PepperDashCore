//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using Crestron.SimplSharp;

//namespace PepperDash.Core
//{
//    public abstract class Feedback
//    {
//        public event EventHandler<EventArgs> OutputChange;

//        public virtual bool BoolValue { get { return false; } }
//        public virtual int IntValue { get { return 0; } }
//        public virtual string StringValue { get { return ""; } }

//        public Cue Cue { get; private set; }

//        public abstract eCueType Type { get; }

//        protected Feedback()
//        {
//        }

//        protected Feedback(Cue cue)
//        {
//            Cue = cue;
//        }

//        public abstract void FireUpdate();

//        protected void OnOutputChange()
//        {
//            if (OutputChange != null) OutputChange(this, EventArgs.Empty);
//        }
//    }

//    public class BoolFeedbackLocalStorage
//    {
//        public bool BoolValue
//        {
//            get { return Output.BoolValue; }
//            set
//            {

//            }
//        }
//        public BoolFeedback Output { get; private set; }

//        public BoolFeedbackLocalStorage(BoolFeedback bo)
//        {
//            Output = bo;
//        }
//    }


//    public class BoolFeedback : Feedback
//    {
//        public override bool BoolValue { get { return ValueFunc.Invoke(); } }

//        public override eCueType Type { get { return eCueType.Bool; } }

//        public Func<bool> ValueFunc { get; private set; }
//        List<BoolInputSig> LinkedInputSigs = new List<BoolInputSig>();
//        List<BoolInputSig> LinkedComplementInputSigs = new List<BoolInputSig>();

//        public BoolFeedback(Func<bool> valueFunc)
//            : this(Cue.DefaultBoolCue, valueFunc)
//        {
//        }

//        public BoolFeedback(Cue cue, Func<bool> valueFunc)
//            : base(cue)
//        {
//            if (cue == null) throw new ArgumentNullException("cue");
//            ValueFunc = valueFunc;
//        }

//        public override void FireUpdate()
//        {
//            LinkedInputSigs.ForEach(s => UpdateSig(s));
//            LinkedComplementInputSigs.ForEach(s => UpdateComplementSig(s));
//            OnOutputChange();
//        }

//        public void LinkInputSig(BoolInputSig sig)
//        {
//            LinkedInputSigs.Add(sig);
//            UpdateSig(sig);
//        }

//        public void UnlinkInputSig(BoolInputSig sig)
//        {
//            LinkedInputSigs.Remove(sig);
//        }

//        public void LinkComplementInputSig(BoolInputSig sig)
//        {
//            LinkedComplementInputSigs.Add(sig);
//            UpdateComplementSig(sig);
//        }

//        public void UnlinkComplementInputSig(BoolInputSig sig)
//        {
//            LinkedComplementInputSigs.Remove(sig);
//        }

//        void UpdateSig(BoolInputSig sig)
//        {
//            sig.BoolValue = ValueFunc.Invoke();
//        }

//        void UpdateComplementSig(BoolInputSig sig)
//        {
//            sig.BoolValue = !ValueFunc.Invoke();
//        }
//    }

//    //******************************************************************************
//    public class IntFeedback : Feedback
//    {
//        public override int IntValue { get { return ValueFunc.Invoke(); } }
//        public ushort UShortValue { get { return (ushort)ValueFunc.Invoke(); } }
//        public override eCueType Type { get { return eCueType.Int; } }

//        Func<int> ValueFunc;
//        List<UShortInputSig> LinkedInputSigs = new List<UShortInputSig>();

//        public IntFeedback(Func<int> valueFunc)
//            : this(Cue.DefaultIntCue, valueFunc)
//        {
//        }

//        public IntFeedback(Cue cue, Func<int> valueFunc)
//            : base(cue)
//        {
//            if (cue == null) throw new ArgumentNullException("cue");
//            ValueFunc = valueFunc;
//        }

//        public override void FireUpdate()
//        {
//            LinkedInputSigs.ForEach(s => UpdateSig(s));
//            OnOutputChange();
//        }

//        public void LinkInputSig(UShortInputSig sig)
//        {
//            LinkedInputSigs.Add(sig);
//            UpdateSig(sig);
//        }

//        public void UnlinkInputSig(UShortInputSig sig)
//        {
//            LinkedInputSigs.Remove(sig);
//        }

//        void UpdateSig(UShortInputSig sig)
//        {
//            sig.UShortValue = this.UShortValue;
//        }
//    }


//    //******************************************************************************
//    public class StringFeedback : Feedback
//    {
//        public override string StringValue { get { return ValueFunc.Invoke(); } }
//        public override eCueType Type { get { return eCueType.String; } }

//        public Func<string> ValueFunc { get; private set; }
//        List<StringInputSig> LinkedInputSigs = new List<StringInputSig>();

//        public StringFeedback(Func<string> valueFunc)
//            : this(Cue.DefaultStringCue, valueFunc)
//        {
//        }

//        public StringFeedback(Cue cue, Func<string> valueFunc)
//            : base(cue)
//        {
//            if (cue == null) throw new ArgumentNullException("cue");
//            ValueFunc = valueFunc;

//        }

//        public override void FireUpdate()
//        {
//            LinkedInputSigs.ForEach(s => UpdateSig(s));
//            OnOutputChange();
//        }

//        public void LinkInputSig(StringInputSig sig)
//        {
//            LinkedInputSigs.Add(sig);
//            UpdateSig(sig);
//        }

//        public void UnlinkInputSig(StringInputSig sig)
//        {
//            LinkedInputSigs.Remove(sig);
//        }

//        void UpdateSig(StringInputSig sig)
//        {
//            sig.StringValue = ValueFunc.Invoke();
//        }
//    }
//}