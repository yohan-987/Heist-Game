using System.IO;
using Game.Economy;
using Game.Save;
using NUnit.Framework;

namespace Game.Tests
{
    public class SaveServiceTests
    {
        private string _dir;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "heist_save_test_" + System.Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
        }

        [Test]
        public void Load_WithNoFile_GivesFreshProfile()
        {
            var svc = new SaveService(_dir);
            Assert.IsFalse(svc.HasSave);

            var p = svc.Load();
            Assert.AreEqual(0f, p.Money);
            Assert.AreEqual(0, p.Upgrades.Count);
        }

        [Test]
        public void SaveThenLoad_RoundTrips()
        {
            var svc = new SaveService(_dir);
            var p = new PlayerProfile { Money = 1234f, HighestLevelReached = 7 };
            p.SetLevel("hp", 3);
            p.SetLevel("stamina", 1);

            svc.Save(p);
            var loaded = new SaveService(_dir).Load();

            Assert.AreEqual(1234f, loaded.Money);
            Assert.AreEqual(7, loaded.HighestLevelReached);
            Assert.AreEqual(3, loaded.GetLevel("hp"));
            Assert.AreEqual(1, loaded.GetLevel("stamina"));
        }

        [Test]
        public void SecondSave_KeepsPreviousAsBackup()
        {
            var svc = new SaveService(_dir);
            svc.Save(new PlayerProfile { Money = 100f });
            svc.Save(new PlayerProfile { Money = 200f });

            Assert.IsTrue(File.Exists(svc.ProfilePath + ".bak"));
            Assert.AreEqual(200f, svc.Load().Money);
        }

        [Test]
        public void CorruptMainFile_FallsBackToBackup()
        {
            var svc = new SaveService(_dir);
            svc.Save(new PlayerProfile { Money = 100f });
            svc.Save(new PlayerProfile { Money = 200f });

            File.WriteAllText(svc.ProfilePath, "{ this is not json");

            Assert.AreEqual(100f, svc.Load().Money);
        }

        [Test]
        public void EverythingCorrupt_GivesFreshProfile_NotACrash()
        {
            var svc = new SaveService(_dir);
            svc.Save(new PlayerProfile { Money = 100f });
            File.WriteAllText(svc.ProfilePath, "garbage");

            Assert.DoesNotThrow(() => svc.Load());
        }

        [Test]
        public void Delete_RemovesEverything()
        {
            var svc = new SaveService(_dir);
            svc.Save(new PlayerProfile());
            svc.Save(new PlayerProfile());
            svc.Delete();
            Assert.IsFalse(svc.HasSave);
        }
    }
}
