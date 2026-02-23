using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[CreateAssetMenu(menuName = "MyScriptable/Create PositionDefinition_SO")]
public class PositionDefinition_SO : ScriptableObject
{
    public List<PositionDefinitionSlot_SO> data = new List<PositionDefinitionSlot_SO>();
} 
