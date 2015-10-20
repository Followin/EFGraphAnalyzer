using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;

namespace EFGraphAnalyzer.Models
{
    public class EFContext : DbContext
    {
        public DbSet<Student> Students { get; set; }
        public DbSet<Course> Courses { get; set; }

        public EFContext()
            : base("DefaultConnection")
        {
            Database.SetInitializer(new EFContextInitializer());
        }



        public enum GraphActionType
        {
            Create,
            Update
        }



        public void AnalyseGraph<T>(T item, Boolean modifyAttachedEntities = true, Boolean checkForRemovedReferences = true, List<Type> typesToCheck = null) where T : class
        {

            GraphActionType action;
            var primarySet = Set<T>();
            var existingItem = primarySet.Find(typeof(T).GetProperty("Id").GetValue(item));

            typesToCheck = typesToCheck ??
                GetType().GetProperties()
                                         .Where(p => p.PropertyType.IsGenericType &&
                                             p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
                                         .Select(p => p.PropertyType.GetGenericArguments().First())
                                         .ToList();
            typesToCheck.Remove(typeof(T));
            var props = item
                .GetType().GetProperties()
                                          .Where(x => x.PropertyType.IsGenericType &&
                                              x.PropertyType.GetGenericTypeDefinition() == typeof(ICollection<>) &&
                                              typesToCheck.Contains(x.PropertyType.GetGenericArguments().Single()))
                                          .ToList();
            if (checkForRemovedReferences)
            {
                foreach (var propertyInfo in props)
                {
                    var propertyType = propertyInfo.PropertyType.GetGenericArguments().Single();
                    var existingCollection = propertyInfo.GetValue(existingItem);
                    var newCollection = propertyInfo.GetValue(item);
                    //var existingCollection = Convert.ChangeType(propertyInfo.GetValue(existingItem), propertyInfo.PropertyType);
                    //var newCollection = Convert.ChangeType(propertyInfo.GetValue(item), propertyInfo.PropertyType);

                    var existingIds = new ArrayList();
                    var newIds = new ArrayList();
                    var idProperty = propertyType.GetProperty("Id");
                    foreach (var collectionItem in (IEnumerable)existingCollection)
                    {
                        existingIds.Add(idProperty.GetValue(collectionItem));
                    }
                    foreach (var collectionItem in (IEnumerable)newCollection)
                    {
                        newIds.Add(idProperty.GetValue(collectionItem));
                    }
                    var existingIdsArray = existingIds.ToArray();
                    var newIdsArray = newIds.ToArray();

                    var exceptionMethod = typeof(Enumerable)
                                                            .GetMethods()
                                                            .SingleOrDefault(x => x.Name == "Except" &&
                                                                                  x.GetParameters().Length == 2 &&
                                                                                  x.GetGenericArguments().Length == 1);
                    var genericExceptMethod = exceptionMethod.MakeGenericMethod(typeof(object));
                    
                    var toListMethod = typeof (Enumerable).GetMethod("ToList");
                    var genericToListMethod = toListMethod.MakeGenericMethod(typeof (object));
                    var idsToRemove = genericExceptMethod.Invoke(null, new object[] { existingIdsArray, newIdsArray });
                    var idsToAdd = genericExceptMethod.Invoke(null, new object[] { newIdsArray, existingIdsArray });
                    var idsToRemoveList = genericToListMethod.Invoke(null, new object[] {idsToRemove});

                    


                }
            }


            if (existingItem == null)
            {
                primarySet.Add(item);
                action = GraphActionType.Create;
            }
            else
            {
                if (typesToCheck == null)
                    Entry(existingItem).State = EntityState.Detached;

                if (modifyAttachedEntities)
                    Entry(item).State = EntityState.Modified;

                else primarySet.Attach(item);

                action = GraphActionType.Update;
            }

            
            foreach (var propertyInfo in props)
            {
                typesToCheck.Remove(propertyInfo.PropertyType.GetGenericArguments().Single());
            }

            foreach (var propertyInfo in props)
            {
                foreach (var collectionItem in (propertyInfo.GetValue(item) as IEnumerable))
                {
                    var typedItem = Convert.ChangeType(collectionItem,
                        propertyInfo.PropertyType.GetGenericArguments().Single());

                    GetType()
                        .GetMethod("AnalyseGraph")
                        .MakeGenericMethod(typedItem.GetType())
                        .Invoke(this, new[] { typedItem, modifyAttachedEntities, checkForRemovedReferences, typesToCheck });
                }
            }








        }
    }

    public class EFContextInitializer : DropCreateDatabaseAlways<EFContext>
    {
        protected override void Seed(EFContext context)
        {
            context.Students.Add(new Student
            {
                Name = "FirstStudent",
                Courses = new[]
                {
                    new Course {Name = "FirstCourse"},
                    new Course {Name = "SecondCourse"}
                }
            });

            context.SaveChanges();
        }
    }
}
