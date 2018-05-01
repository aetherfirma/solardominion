using System;
using FullSerializer;

namespace Logic.Utilities
{
    public static class StringSerializationAPI {
        private static readonly fsSerializer _serializer = new fsSerializer();

        public static string Serialize<T>(object value) {
            // serialize the data
            fsData data;
            _serializer.TrySerialize(typeof(T), value, out data).AssertSuccessWithoutWarnings();

            // emit the data via JSON
            return fsJsonPrinter.CompressedJson(data);
        }

        public static T Deserialize<T>(string serializedState) {
            // step 1: parse the JSON data
            var data = fsJsonParser.Parse(serializedState);

            // step 2: deserialize the data
            object deserialized = null;
            _serializer.TryDeserialize(data, typeof(T), ref deserialized).AssertSuccessWithoutWarnings();

            return (T) deserialized;
        }
    }}