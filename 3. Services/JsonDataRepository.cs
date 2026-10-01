using System;
using System.IO;
using Newtonsoft.Json;
using LostArkCalculator.Core;
using LostArkCalculator.Models;

namespace LostArkCalculator.Services
{
    public class JsonDataRepository : IDataRepository
    {
        public UserData Data { get; private set; }

        public JsonDataRepository()
        {
            try
            {
                if (File.Exists(AppConstants.SaveFilePath))
                    Data = JsonConvert.DeserializeObject<UserData>(File.ReadAllText(AppConstants.SaveFilePath));
                
                if (Data == null || Data.Characters == null || Data.Characters.Count == 0) throw new Exception();
            }
            catch
            {
                Data = new UserData();
                Data.Characters["본캐"] = new Character();
            }
        }

        public void Save() => File.WriteAllText(AppConstants.SaveFilePath, JsonConvert.SerializeObject(Data, Formatting.Indented));
    }
}