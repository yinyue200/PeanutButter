﻿using System;

namespace PeanutButter.DuckTyping.Exceptions
{
    public class BackingFieldForPropertyNotFoundException : NotImplementedException
    {
        public BackingFieldForPropertyNotFoundException(Type owningType, string propertyName)
            : base($"Public property {propertyName} not found on type {owningType.Name}")
        {
        }
    }
}