using AICareerCoach.DAL.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace AICareerCoach.DAL.repository
{
    public class GenericRepo<T>:IBaserepo<T> where T : class
    {
        //will be updated to right dbcontext name
        private readonly AICareerCoachDbContext context;
        private readonly DbSet<T> dbset;
        public GenericRepo(AICareerCoachDbContext _context)
        {
            context = _context;
            dbset = context.Set<T>();
        }

        public List<T> Getall()
        {
            return dbset.ToList();
        }

         public T? GetbyId(int id)
        {
            return dbset.Find(id);
        }

        void IBaserepo<T>.Add(T item)
        {
            dbset.Add(item);
            context.SaveChanges();
        }

        void IBaserepo<T>.Delete(T item)
        {
            dbset.Remove(item);
            context.SaveChanges();
        }

        void IBaserepo<T>.Update(T item)
        {
            dbset.Update(item);   
            context.SaveChanges();
        }

        


    }
}
