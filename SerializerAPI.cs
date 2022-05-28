using System;
using System.Text.Json;

namespace MarvelousAPI
{
    public class MethodWrapper<T>
    {
        public string MethodType { get { return typeof(T).Name; } }
        public T Contents { get; set; }
    }

    public class MethodWrapper
    {
        public string MethodType { get; set; }
        public object Contents { get; set; }
    }

    public class SerializerAPI
    {
        public string Serialize(dynamic instance)
        {
            string typeName = instance?.GetType().ToString();
            Type typeArgument = Type.GetType(typeName);
            Type genericClass = typeof(MethodWrapper<>);
            Type constructedClass = genericClass.MakeGenericType(typeArgument);
            dynamic created = Activator.CreateInstance(constructedClass);
            created.Contents = instance;
            JsonSerializerOptions options = new() { WriteIndented = true };
            return JsonSerializer.Serialize(created, options);
        }

        public dynamic Deserialize(string json)
        {
            MethodWrapper deserializedMethod = JsonSerializer.Deserialize<MethodWrapper>(json);
            return JsonSerializer.Deserialize(Convert.ToString(deserializedMethod.Contents), Type.GetType($"MarvelousAPI.{deserializedMethod.MethodType}"));
        }

        public SerializerAPI()
        {

        }
    }
}
