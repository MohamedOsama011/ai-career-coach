using AICareerCoach.API.Response;
using AICareerCoach.BLL.DTO.User;
using AICareerCoach.BLL.Services;
using AICareerCoach.DAL.Entities;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AICareerCoach.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IBaseservice<User> _userService;


        public UserController(IBaseservice<User> baseservice)
        {
            _userService = baseservice;
        }

        //get all
        [HttpGet]
        public GeneralResponse Getall()
        {
            return await _context.Users.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(int id)
        {
            var user = await _context.Users.FindAsync(id);

            }
            return response;
        }
       //add user
        [HttpPost]
        public IActionResult AddUser([FromBody]Add user)
        {
            var user1 = new User();
            user1.FullName = user.Name;
            user1.Email = user.email;
            _userService.Add(user1);
            return CreatedAtAction("GetUser ", new{ id = user1.Id }, user1);
            //return Created("created successfully", user1);
        }
        //delete 
        [HttpDelete("{id}")]
        public GeneralResponse    DeleteUser(int id)
        {
            GeneralResponse generalResponse=new GeneralResponse();
            var user = _userService.GetbyId(id);
            if (user != null)
            {
                _userService.Delete(user);
                generalResponse.Sucess = true;
                generalResponse.Data = "deleted sucessfully";
            }
            else
            { 
                generalResponse.Sucess = false;
                generalResponse.Data = "not found";//should be handeled more
            }
            return generalResponse;
        }
        //update
        [HttpPut("{id:int}")]
        public GeneralResponse Edit ([FromBody] Update user1,int id)
        {
            var generalResponse=new GeneralResponse();
            var user= _userService.GetbyId(id);
            if(user!=null)
            {
                user.Email= user1.Email;
                user.FullName=user1.FullName;
                _userService.Update(user);
                generalResponse.Sucess = true;
                generalResponse.Data = "user updated successfly";
            }
            else
            {
                generalResponse.Sucess = false;
                generalResponse.Data = "user not fouond";
			}
            return generalResponse;
		}

    }
}
