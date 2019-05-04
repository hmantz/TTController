﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TTController.Service.Config.Converter
{
    public abstract class AbstractNamedValueTupleConverter<T1, T2> : JsonConverter<ValueTuple<T1, T2>>
    {
        public override void WriteJson(JsonWriter writer, ValueTuple<T1, T2> value, JsonSerializer serializer)
        {
            var o = new JObject
            {
                { KeyName(), JsonConvert.SerializeObject(value.Item1) },
                { ValueName(), JsonConvert.SerializeObject(value.Item2) }
            };
            o.WriteTo(writer);
        }

        public override ValueTuple<T1, T2> ReadJson(JsonReader reader, Type objectType, ValueTuple<T1, T2> existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            var o = JObject.Load(reader);

            var v1 = (T1) JsonConvert.DeserializeObject(o[KeyName()].ToString(), typeof(T1));
            var v2 = (T2) JsonConvert.DeserializeObject(o[ValueName()].ToString(), typeof(T2));

            return new ValueTuple<T1, T2>(v1, v2);
        }

        protected abstract string KeyName();
        protected abstract string ValueName();
    }
}
