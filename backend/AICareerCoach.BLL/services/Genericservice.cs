using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AICareerCoach.DAL.repository;

namespace AICareerCoach.BLL.Services
{
    //any logic will be here  
    public class Genericservice<T>:IBaseservice<T>   where T : class 
    {
        private readonly IBaserepo<T> baserepo;
        public Genericservice(IBaserepo<T> _baserepo) 
        { 
        
         this.baserepo = _baserepo;
        }

        public List<T>? Getall()
        {
            List<T>? list = baserepo.Getall();
            if(list == null)
                return null;
            return list;
        }

        public T? GetbyId(int id)
        {
            var x = baserepo.GetbyId(id);
            if(x == null) return null;
            return x;
            
        }
        public void Add(T item)
        {
            baserepo.Add(item);
        }

        public void Delete(T item)
        {
                baserepo.Delete(item);
        }
        public void Update(T item)
        {
            baserepo.Update(item);
        }
    }
}
