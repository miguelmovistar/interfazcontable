//------------------------------------------------------------------------------
// <auto-generated>
//     Este código se generó a partir de una plantilla.
//
//     Los cambios manuales en este archivo pueden causar un comportamiento inesperado de la aplicación.
//     Los cambios manuales en este archivo se sobrescribirán si se regenera el código.
// </auto-generated>
//------------------------------------------------------------------------------

namespace IC2.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class Trafico
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Trafico()
        {
            this.BonoTrafico = new HashSet<BonoTrafico>();
            this.CaracteristicasPA = new HashSet<CaracteristicasPA>();
            this.CentroCosto = new HashSet<CentroCosto>();
            this.Concepto = new HashSet<Concepto>();
            this.CuentaResultado = new HashSet<CuentaResultado>();
            this.Objecion = new HashSet<Objecion>();
            this.Tarifa = new HashSet<Tarifa>();
            this.Tarifa_Fee = new HashSet<Tarifa_Fee>();
        }
    
        public int Id { get; set; }
        public string Id_TraficoTR { get; set; }
        public string Sentido { get; set; }
        public string Descripcion { get; set; }
        public int Activo { get; set; }
        public int Id_ServicioTR { get; set; }
        public Nullable<int> Id_LineaNegocio { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<BonoTrafico> BonoTrafico { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<CaracteristicasPA> CaracteristicasPA { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<CentroCosto> CentroCosto { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Concepto> Concepto { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<CuentaResultado> CuentaResultado { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Objecion> Objecion { get; set; }
        public virtual Servicio Servicio { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Tarifa> Tarifa { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Tarifa_Fee> Tarifa_Fee { get; set; }
    }
}
