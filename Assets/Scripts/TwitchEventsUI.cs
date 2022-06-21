using System.Collections;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using TwitchLib.PubSub;
using TwitchLib.PubSub.Events;

public class TwitchEvent
{
    public int Version { get; set; }
    public string Trigger { get; set; }
    public List<TwitchCondition> Conditions { get; set; }
    public string Action { get; set; }
    public Dictionary<string, string> Arguments { get; set; }
}

public class TwitchArgument
{
    public string ArgumentName { get; set; }
    public int ValueType { get; set; }
    public string Value { get; set; }
    public bool IsProperty { get; set; }
}

public class TwitchCondition
{
    public string Property { get; set; }
    public int Operation { get; set; }
    public string Value { get; set; }
    public int ValueType { get; set; }
}

public class TwitchProperty
{
    public string Name { get; set; }
    public string Property { get; set; }
    public TwitchEventsUI.ValueType ValueType { get; set; }
}

public class TwitchEventsUI : MonoBehaviour
{
    public TwitchMaster twitchMaster;

    public List<TwitchProperty> RewardRedeemedProperties = new List<TwitchProperty>
    {
        new TwitchProperty{Name = "Username", Property = "USERNAME", ValueType = ValueType.String },
        new TwitchProperty{Name = "Reward Name", Property = "REWARDNAME", ValueType = ValueType.String },
        new TwitchProperty{Name = "User Subscription Tier", Property = "USERSUBTIER", ValueType = ValueType.Integer },
        new TwitchProperty{Name = "Reward Cost", Property = "REWARDCOST", ValueType = ValueType.Integer },
        new TwitchProperty{Name = "User Subbed", Property = "USERSUBBED", ValueType = ValueType.Bool },
        new TwitchProperty{Name = "User Follows", Property = "USERFOLLOWS", ValueType = ValueType.Bool },
    };
    public enum StringOperation
    {
        Equals = 0,
        NotEquals = 1,
        Contains = 2,
        NotContains = 3,
        StartsWith = 4,
        NotStartsWith = 5,
        EndsWith = 6,
        NotEndsWith = 7,
    }
    public enum IntegerOperation
    {
        Equals = 0,
        NotEquals = 1,
        GreaterThan = 2,
        GreaterThanOrEqual = 3,
        LessThan = 4,
        LessThanOrEqual = 5,
    }
    public enum FloatOperation
    {
        Equals = 0,
        NotEquals = 1,
        GreaterThan = 2,
        GreaterThanOrEqual = 3,
        LessThan = 4,
        LessThanOrEqual = 5,
    }

    public enum BoolOperation
    {
        IsTrue = 0,
        IsFalse = 1,
    }

    public enum ValueType
    {
        String = 0,
        Integer = 1,
        Float = 2,
        Bool = 3,
    }
    public bool CheckString(string s1, string s2, int operation)
    {
        StringOperation op = (StringOperation)operation;
        Dictionary<StringOperation, bool> checks = new Dictionary<StringOperation, bool>
        {
            {StringOperation.Equals, s1.Equals(s2) },
            {StringOperation.NotEquals, !s1.Equals(s2) },
            {StringOperation.Contains, s1.Contains(s2) },
            {StringOperation.NotContains, !s1.Contains(s2) },
            {StringOperation.StartsWith, s1.StartsWith(s2) },
            {StringOperation.NotStartsWith, !s1.StartsWith(s2) },
            {StringOperation.EndsWith, s1.EndsWith(s2) },
            {StringOperation.NotEndsWith, !s1.EndsWith(s2) },
        };
        return checks[op];
    }
    public bool CheckFloat(float f1, float f2, int operation)
    {
        FloatOperation op = (FloatOperation)operation;
        Dictionary<FloatOperation, bool> checks = new Dictionary<FloatOperation, bool>
        {
            {FloatOperation.Equals, f1 == f2},
            {FloatOperation.NotEquals, f1 != f2},
            {FloatOperation.GreaterThan, f1 > f2},
            {FloatOperation.GreaterThanOrEqual, f1 >= f2},
            {FloatOperation.LessThan, f1 < f2},
            {FloatOperation.LessThanOrEqual, f1 <= f2},
        };
        return checks[op];
    }
    public bool CheckInteger(float i1, float i2, int operation)
    {
        IntegerOperation op = (IntegerOperation)operation;
        Dictionary<IntegerOperation, bool> checks = new Dictionary<IntegerOperation, bool>
        {
            {IntegerOperation.Equals, i1 == i2},
            {IntegerOperation.NotEquals, i1 != i2},
            {IntegerOperation.GreaterThan, i1 > i2},
            {IntegerOperation.GreaterThanOrEqual, i1 >= i2},
            {IntegerOperation.LessThan, i1 < i2},
            {IntegerOperation.LessThanOrEqual, i1 <= i2},
        };
        return checks[op];
    }
    public bool CheckCondition<T>(TwitchCondition condition, T args)
    {
        ValueType valType = (ValueType)condition.ValueType;
        switch (valType)
        {
            case ValueType.String:
                string propertyString = HelperFunctions.GetPropertyString(args, condition.Property, twitchMaster);
                return CheckString(propertyString, condition.Value, condition.Operation);
            case ValueType.Integer:
                int propertyInteger = HelperFunctions.GetPropertyInteger(args, condition.Property, twitchMaster);
                return CheckInteger(propertyInteger, int.Parse(condition.Value), condition.Operation);
            case ValueType.Float:
                float propertyFloat = HelperFunctions.GetPropertyFloat(args, condition.Property, twitchMaster);
                return CheckFloat(propertyFloat, float.Parse(condition.Value), condition.Operation);
            case ValueType.Bool:
                return (((BoolOperation)condition.Operation) == BoolOperation.IsFalse) ^ HelperFunctions.GetPropertyBool(args, condition.Property, twitchMaster);
            default:
                throw new NotImplementedException();
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
