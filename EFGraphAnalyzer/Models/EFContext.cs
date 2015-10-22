using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using EFGraphAnalyzer.Utils.ArrayExtensions;

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
            
            if (existingItem != null && checkForRemovedReferences)
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

                    var exceptMethod = typeof(Enumerable)
                                                         .GetMethods()
                                                         .SingleOrDefault(x => x.Name == "Except" &&
                                                                               x.GetParameters().Length == 2 &&
                                                                               x.GetGenericArguments().Length == 1);
                    var genericExceptMethod = exceptMethod.MakeGenericMethod(typeof(object));
                    var intersectMethod = typeof(Enumerable)
                                                            .GetMethods()
                                                            .SingleOrDefault(x => x.Name == "Intersect" &&
                                                                                  x.GetParameters().Length == 2 &&
                                                                                  x.GetGenericArguments().Length == 1);
                    
                    
                    var toListMethod = typeof (Enumerable).GetMethod("ToList");
                    var idsToRemove = genericExceptMethod.Invoke(null, new object[] { existingIdsArray, newIdsArray });
                    var idsToAdd = genericExceptMethod.Invoke(null, new object[] { newIdsArray, existingIdsArray });
                    var containsMethod = typeof(Enumerable)
                                                           .GetMethods()
                                                           .SingleOrDefault(x => x.Name == "Contains" &&
                                                                                 x.GetParameters().Length == 2 &&
                                                                                 x.GetGenericArguments().Length == 1);
                    var genericContainsMethod = containsMethod.MakeGenericMethod(typeof(object));
                    var countMethod = typeof(Enumerable)
                                                        .GetMethods()
                                                        .SingleOrDefault(x => x.Name == "Count" &&
                                                                              x.GetParameters().Length == 1 &&
                                                                              x.GetGenericArguments().Length == 1);
                    var genericCountMethod = countMethod.MakeGenericMethod(propertyType);
                    var elementAtMethod = typeof(Enumerable)
                                                            .GetMethods()
                                                            .SingleOrDefault(x => x.Name == "ElementAt" &&
                                                                                  x.GetParameters().Length == 2 &&
                                                                                  x.GetGenericArguments().Length == 1);
                    var genericElementAtMethod = elementAtMethod.MakeGenericMethod(propertyType);
                    for (Int32 i = 0; i < (int) genericCountMethod.Invoke(null, new[] {existingCollection});)
                    {
                        var collectionItem = genericElementAtMethod.Invoke(null, new object[] {(IEnumerable)existingCollection, i});
                        if (
                            (bool)
                                genericContainsMethod.Invoke(null,
                                    new[] {idsToRemove, idProperty.GetValue(collectionItem)}))
                        {
                            var collectionType = typeof (ICollection<>).MakeGenericType(propertyType);
                            var removeMethod = collectionType.GetMethod("Remove", new[] {propertyType});
                            removeMethod.Invoke(existingCollection, new[] {collectionItem});
                        }
                        else
                        {
                            i++;
                        }
                    }
                    
                    //Entry(existingItem).State = EntityState.Detached;
                    
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
                {
                     Entry(existingItem).State = EntityState.Modified;
                }  

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
