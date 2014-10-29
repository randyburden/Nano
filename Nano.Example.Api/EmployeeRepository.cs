using System;
using System.Collections.Generic;
using System.Linq;

namespace Nano.Example.Api
{
    /// <summary>
    /// Provides methods for interacting with employee records.
    /// </summary>
    public class EmployeeRepository
    {
        /// <summary>
        /// Employee records.
        /// </summary>
        /// <remarks>
        /// This is intended to simulate a database / repository of employee records.
        /// </remarks>
        readonly static List<Employee> Employees = new List<Employee>(); 

        /// <summary>
        /// Adds an employee.
        /// </summary>
        /// <param name="firstName">Employee first name.</param>
        /// <param name="lastName">Employee last name.</param>
        /// <returns>Employee record.</returns>
        public static Employee AddEmployee( string firstName, string lastName )
        {
            var employee = new Employee();

            employee.EmployeeId = Employees.Count == 0 ? 1 : Employees.Max( x => x.EmployeeId ) + 1;

            employee.FirstName = firstName.Trim();

            employee.LastName = lastName.Trim();

            Employees.Add( employee );

            return employee;
        }

        /// <summary>
        /// Adds an employee record and allows the user to specify the employee id.
        /// </summary>
        /// <param name="employee">Employee record.</param>
        /// <returns>Employee record.</returns>
        public static Employee AddEmployeeRecord( Employee employee )
        {
            if ( Employees.Exists( x => x.EmployeeId == employee.EmployeeId ) )
                throw new Exception( string.Format( "Employee {0} already exist", employee.EmployeeId ) );

            Employees.Add( employee );

            return employee;
        }

        /// <summary>
        /// Gets an employee by id.
        /// </summary>
        /// <param name="id">Employee id.</param>
        /// <returns>Employee record.</returns>
        public static Employee GetEmployeeById( int id )
        {
            var employee = Employees.FirstOrDefault( x => x.EmployeeId == id );

            return employee;
        }

        /// <summary>
        /// Gets an employee by name.
        /// </summary>
        /// <param name="firstName">Employee first name.</param>
        /// <param name="lastName">Employee last name.</param>
        /// <returns>Employee record.</returns>
        public static Employee GetEmployeeByName( string firstName, string lastName )
        {
            var employee = Employees.FirstOrDefault( x => x.FirstName.ToLower() == firstName.Trim().ToLower() &&
                                                          x.LastName.ToLower() == lastName.Trim().ToLower() );

            return employee;
        }
    }

    /// <summary>
    /// An employee.
    /// </summary>
    public class Employee
    {
        /// <summary>
        /// Employee Id.
        /// </summary>
        public int EmployeeId;

        /// <summary>
        /// Employees first name.
        /// </summary>
        public string FirstName;

        /// <summary>
        /// Employees last name.
        /// </summary>
        public string LastName;
    }
}