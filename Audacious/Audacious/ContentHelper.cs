using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Audacious
{
    public class ContentHelper
    {
        const int MAX_SOUNDEFFECT_COUNT = 3;
        static Dictionary<string, object> dicContent = new Dictionary<string, object>();
        static Dictionary<string, List<SoundEffectInstance>> dicSoundEffectInstancePools = new Dictionary<string, List<SoundEffectInstance>>();
        static Dictionary<string, int> dicSoundEffectInstanceIndex = new Dictionary<string, int>();
        static ContentHelper instance;
        ContentManager contentManager;

        private ContentHelper()
        {
        }

        public static void Setup(ContentManager content)
        {
            Instance.contentManager = content;
        }

        public static ContentHelper Instance
        {
            get
            {
                if (instance == null)
                    instance = new ContentHelper();

                return instance;
            }
        }

        public T GetContent<T>(string name)
        {
            if (dicContent.ContainsKey(name))
                return (T)dicContent[name];
            else
            {
                var content = contentManager.Load<T>(name);
                dicContent.Add(name, content);
                return content;
            }
        }

        public SoundEffectInstance GetSoundEffectInstance(string name)
        {
            List<SoundEffectInstance> soundEffectInstancePool = null;
            SoundEffectInstance soundEffectInstance = null;

            if (dicSoundEffectInstancePools.ContainsKey(name))
            {
                soundEffectInstancePool = dicSoundEffectInstancePools[name];
            }
            else
            {
                dicSoundEffectInstanceIndex.Add(name, 0);
                soundEffectInstancePool = new List<SoundEffectInstance>();
                dicSoundEffectInstancePools.Add(name, soundEffectInstancePool);
            }

            if (soundEffectInstancePool.Count() < MAX_SOUNDEFFECT_COUNT)
            {
                var soundEffect = GetContent<SoundEffect>(name);
                soundEffectInstance = soundEffect.CreateInstance();
                soundEffectInstancePool.Add(soundEffectInstance);
            }
            else
            {
                soundEffectInstance = soundEffectInstancePool[dicSoundEffectInstanceIndex[name]++];
            }

            if (dicSoundEffectInstanceIndex[name] >= MAX_SOUNDEFFECT_COUNT)
                dicSoundEffectInstanceIndex[name] = 0;

            return soundEffectInstance;
        }

        public void Unload()
        {
            dicContent.Clear();
            contentManager.Unload();
        }
    }
}
