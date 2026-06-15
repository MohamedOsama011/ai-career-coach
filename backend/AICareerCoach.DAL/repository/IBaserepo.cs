using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AICareerCoach.DAL.repository
{
    public interface IBaserepo<T> where T : class
    {
        public List<T>? Getall();
        public T? GetbyId(int id);
        public void Add(T item);
        public void Delete(T item);
        public void Update(T item);

    }
}
