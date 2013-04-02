using System.Data;
using ServiceStack.OrmLite;
using UniPhotoGallery.DomainModel.Auth;
using UniPhotoGallery.DomainModel.Domain;

namespace UniPhotoGallery.Data
{
    public interface IDBSetupRepository
    {
        void SetupDatabase();
        void ForceSetupDatabase();
    }
    
    public class DBSetupRepository : BaseRepository, IDBSetupRepository
    {
        public void SetupDatabase()
        {
            using (IDbConnection conn = _db.OpenDbConnection())
            {
                conn.CreateTableIfNotExists<User>();
                conn.CreateTableIfNotExists<Owner>();

                if (!conn.TableExists("GalleryType"))
                {
                    conn.CreateTable<GalleryType>();

                    PopulateGalleryTypeTable(conn);
                }

                if (!conn.TableExists("PhotoType"))
                {
                    conn.CreateTable<PhotoType>();
                    PopulatePhotoTypeTable(conn);
                }

                conn.CreateTableIfNotExists<Gallery>();
                conn.CreateTableIfNotExists<Photo>();

                conn.CreateTableIfNotExists<GalleryPhoto>();
            }
        }

        public void ForceSetupDatabase()
        {
            using (IDbConnection conn = _db.OpenDbConnection())
            {
                conn.DropAndCreateTable<User>();
                conn.DropAndCreateTable<Owner>();
                
                conn.DropAndCreateTable<GalleryType>();
                PopulateGalleryTypeTable(conn);

                conn.DropAndCreateTable<PhotoType>();
                PopulatePhotoTypeTable(conn);

                conn.DropAndCreateTable<Gallery>();
                conn.DropAndCreateTable<Photo>();
                conn.DropAndCreateTable<GalleryPhoto>();
            }
        }

        private void PopulateGalleryTypeTable(IDbConnection conn)
        {
            conn.Insert(new GalleryType { GalleryTypeId = 1, GalleryTypeName = "Root" });
            conn.Insert(new GalleryType { GalleryTypeId = 2, GalleryTypeName = "Trash" });
            conn.Insert(new GalleryType { GalleryTypeId = 3, GalleryTypeName = "Preview" });
            conn.Insert(new GalleryType { GalleryTypeId = 4, GalleryTypeName = "Content" });
        }

        private void PopulatePhotoTypeTable(IDbConnection conn)
        {
            conn.Insert(new PhotoType { Name = "Originál", SystemName = "orig", Directory = "orig", X = 1000 });
            conn.Insert(new PhotoType { Name = "Náhledový thumbnail", SystemName = "minithumb", Directory = "minithumb", X = 180, Y = 120 });
            conn.Insert(new PhotoType { Name = "Upload", SystemName = "Upload", Directory = "Upload", X = 1000 });
            conn.Insert(new PhotoType { Name = "Admin thumbnail", SystemName = "adminthumb", Directory = "adminthumb", X = 100, Y = 66 });
            conn.Insert(new PhotoType { Name = "Šířka 1000", SystemName = "w1000", Directory = "w1000", X = 1000 });
            conn.Insert(new PhotoType { Name = "Šířka 800", SystemName = "w800", Directory = "w800", X = 800 });
            conn.Insert(new PhotoType { Name = "Šířka 400", SystemName = "w400", Directory = "w400", X = 400 });
            conn.Insert(new PhotoType { Name = "Čtverec 200 - náhled galerií", SystemName = "square200", Directory = "square200", X = 200, Y = 200 });
        }
    }
}