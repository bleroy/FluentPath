// Copyright © 2010-2015 Bertrand Le Roy.  All Rights Reserved.
// This code released under the terms of the 
// MIT License http://opensource.org/licenses/MIT

namespace Fluent.IO {
    using System;
    using System.ComponentModel;

    public class PathConverter : TypeConverter {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) {
            return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value) {
            var valueString = value as string;
            return valueString != null ? new Path(valueString) : base.ConvertFrom(context, culture, value);
        }
    }
}
