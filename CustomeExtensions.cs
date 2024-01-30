using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace InfoBroker
{
    public static class StringExtension
    {
        public static string ToInteger(this string dateValue)
        {
            string val = dateValue;

            try
            {
                DateTime startDate = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

                DateTime realDate = DateTime.Parse(val);

                string tendigit = realDate.Subtract(startDate).TotalSeconds.ToString();
                string[] a = tendigit.Split(new char[] { '.' });
                val = a[0];
            }
            catch (Exception ex)
            {

                Console.WriteLine("Unable to convert to ten digit");

            }


            return val;
        }

        public static string LastTwoCharacters(this string dateValue)
        {
            string val = dateValue;

            try
            {
                 val = val.TrimEnd().Substring(val.TrimEnd().Length -2,2);
            }
            catch (Exception ex)
            {

                Console.WriteLine("Unable to extract last two characters");

            }


            return val;
        }

        public static string LastFourCharacters(this string dateValue)
        {
            string val = dateValue;

            try
            {
                val = val.TrimEnd().Substring(val.TrimEnd().Length - 4, 4);
            }
            catch (Exception ex)
            {

                Console.WriteLine("Unable to extract last four characters");

            }


            return val;
        }

    }




}