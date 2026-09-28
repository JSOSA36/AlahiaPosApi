using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace PrinterLibrary
{
    public class ValidateImpuestos:IValidateIMpuesto
    {



        public IParametroConfig _repository;

        public ValidateImpuestos(IParametroConfig repository)
        {
                this._repository = repository;
        }
        public  double SetItbis(bool IsItbis, double PrecioVenta)
        {
            double ValorItbis = 0;

            var GetConfg = _repository.GetVyExpression();
            if (GetConfg != null && GetConfg.Valor=="Checked") 
            {
            

                if (IsItbis == true)
                {
                    ValorItbis = PrecioVenta
                   / 1.18 -
                  PrecioVenta;

                    return ValorItbis;

                }
                else
                {
                    ValorItbis = PrecioVenta
                   * 18 / 100;

                    return Math.Abs(ValorItbis);

                }

            }
            else
            {



                return 0;


            }
        }
    }
}
