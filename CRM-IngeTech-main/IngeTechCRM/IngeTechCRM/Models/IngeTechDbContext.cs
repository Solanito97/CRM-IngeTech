using Microsoft.EntityFrameworkCore;
using System;
using System.Text.RegularExpressions;

namespace IngeTechCRM.Models
{
    public class IngeTechDbContext : DbContext
    {
        public IngeTechDbContext(DbContextOptions<IngeTechDbContext> options)
            : base(options)
        {
        }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<TipoUsuario> TiposUsuario { get; set; }
        public DbSet<Provincia> Provincias { get; set; }
        public DbSet<Categoria> Categorias { get; set; }
        public DbSet<Marca> Marcas { get; set; }
        public DbSet<Proveedor> Proveedores { get; set; }
        public DbSet<Producto> Productos { get; set; }
        public DbSet<ImagenProducto> ImagenesProducto { get; set; }
        public DbSet<Almacen> Almacenes { get; set; }
        public DbSet<Inventario> Inventarios { get; set; }
        public DbSet<MovimientoInventario> MovimientosInventario { get; set; }
        public DbSet<AlertaInventario> AlertasInventario { get; set; }
        public DbSet<Comunicado> Comunicados { get; set; }
        public DbSet<SegmentoComunicado> SegmentosComunicado { get; set; }
        public DbSet<EnvioComunicado> EnviosComunicado { get; set; }
        public DbSet<Carrito> Carritos { get; set; }
        public DbSet<ItemCarrito> ItemsCarrito { get; set; }
        public DbSet<Pedido> Pedidos { get; set; }
        public DbSet<DetallePedido> DetallesPedido { get; set; }
        public DbSet<ResetPasswordToken> ResetPasswordTokens { get; set; }

        // Nuevas entidades para chatbot WhatsApp
        public DbSet<ConversacionWhatsApp> ConversacionesWhatsApp { get; set; }
        public DbSet<MensajeWhatsApp> MensajesWhatsApp { get; set; }
        public DbSet<ConfiguracionChatbot> ConfiguracionesChatbot { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuración de nombres de tablas
            modelBuilder.Entity<Usuario>().ToTable("CRM_USUARIO");
            modelBuilder.Entity<TipoUsuario>().ToTable("CRM_TIPO_USUARIO");
            modelBuilder.Entity<Provincia>().ToTable("CRM_PROVINCIA");
            modelBuilder.Entity<Categoria>().ToTable("CRM_CATEGORIA");
            modelBuilder.Entity<Marca>().ToTable("CRM_MARCA");
            modelBuilder.Entity<Proveedor>().ToTable("CRM_PROVEEDOR");
            modelBuilder.Entity<Producto>().ToTable("CRM_PRODUCTO");
            modelBuilder.Entity<ImagenProducto>().ToTable("CRM_IMAGEN_PRODUCTO");
            modelBuilder.Entity<Almacen>().ToTable("CRM_ALMACEN");
            modelBuilder.Entity<Inventario>().ToTable("CRM_INVENTARIO");
            modelBuilder.Entity<MovimientoInventario>().ToTable("CRM_MOVIMIENTO_INVENTARIO");
            modelBuilder.Entity<AlertaInventario>().ToTable("CRM_ALERTA_INVENTARIO");
            modelBuilder.Entity<Comunicado>().ToTable("CRM_COMUNICADO");
            modelBuilder.Entity<SegmentoComunicado>().ToTable("CRM_SEGMENTO_COMUNICADO");
            modelBuilder.Entity<EnvioComunicado>().ToTable("CRM_ENVIO_COMUNICADO");
            modelBuilder.Entity<Carrito>().ToTable("CRM_CARRITO");
            modelBuilder.Entity<ItemCarrito>().ToTable("CRM_ITEM_CARRITO");
            modelBuilder.Entity<Pedido>().ToTable("CRM_PEDIDO");
            modelBuilder.Entity<DetallePedido>().ToTable("CRM_DETALLE_PEDIDO");
            modelBuilder.Entity<ResetPasswordToken>().ToTable("CRM_RESET_PASSWORD_TOKEN");

            // Nombres de tablas para las entidades de WhatsApp
            modelBuilder.Entity<ConversacionWhatsApp>().ToTable("CRM_CONVERSACION_WHATSAPP");
            modelBuilder.Entity<MensajeWhatsApp>().ToTable("CRM_MENSAJE_WHATSAPP");
            modelBuilder.Entity<ConfiguracionChatbot>().ToTable("CRM_CONFIGURACION_CHATBOT");

            // Configuración de claves primarias
            modelBuilder.Entity<Usuario>().HasKey(u => u.IDENTIFICACION);
            modelBuilder.Entity<TipoUsuario>().HasKey(t => t.ID_TIPO_USUARIO);
            modelBuilder.Entity<Provincia>().HasKey(p => p.ID_PROVINCIA);
            modelBuilder.Entity<Categoria>().HasKey(c => c.ID_CATEGORIA);
            modelBuilder.Entity<Marca>().HasKey(m => m.ID_MARCA);
            modelBuilder.Entity<Proveedor>().HasKey(p => p.ID_PROVEEDOR);
            modelBuilder.Entity<Producto>().HasKey(p => p.ID_PRODUCTO);
            modelBuilder.Entity<ImagenProducto>().HasKey(i => i.ID_IMAGEN);
            modelBuilder.Entity<Almacen>().HasKey(a => a.ID_ALMACEN);
            modelBuilder.Entity<Inventario>().HasKey(i => i.ID_INVENTARIO);
            modelBuilder.Entity<MovimientoInventario>().HasKey(m => m.ID_MOVIMIENTO);
            modelBuilder.Entity<AlertaInventario>().HasKey(a => a.ID_ALERTA);
            modelBuilder.Entity<Comunicado>().HasKey(c => c.ID_COMUNICADO);
            modelBuilder.Entity<SegmentoComunicado>().HasKey(s => s.ID_SEGMENTO);
            modelBuilder.Entity<EnvioComunicado>().HasKey(e => e.ID_ENVIO);
            modelBuilder.Entity<Carrito>().HasKey(c => c.ID_CARRITO);
            modelBuilder.Entity<ItemCarrito>().HasKey(i => i.ID_ITEM);
            modelBuilder.Entity<Pedido>().HasKey(p => p.ID_PEDIDO);
            modelBuilder.Entity<DetallePedido>().HasKey(d => d.ID_DETALLE);
            modelBuilder.Entity<ResetPasswordToken>().HasKey(r => r.ID);

            // Claves primarias para las entidades de WhatsApp
            modelBuilder.Entity<ConversacionWhatsApp>().HasKey(c => c.ID_CONVERSACION);
            modelBuilder.Entity<MensajeWhatsApp>().HasKey(m => m.ID_MENSAJE);
            modelBuilder.Entity<ConfiguracionChatbot>().HasKey(c => c.ID_CONFIGURACION);

            // Configuración de relaciones

            // Usuario
            modelBuilder.Entity<Usuario>()
                .HasOne(u => u.Provincia)
                .WithMany(p => p.Usuarios)
                .HasForeignKey(u => u.ID_PROVINCIA)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Usuario>()
                .HasOne(u => u.TipoUsuario)
                .WithMany(t => t.Usuarios)
                .HasForeignKey(u => u.ID_TIPO_USUARIO)
                .OnDelete(DeleteBehavior.Restrict);

            // Producto
            modelBuilder.Entity<Producto>()
                .HasOne(p => p.Categoria)
                .WithMany(c => c.Productos)
                .HasForeignKey(p => p.ID_CATEGORIA)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Producto>()
                .HasOne(p => p.Marca)
                .WithMany(m => m.Productos)
                .HasForeignKey(p => p.ID_MARCA)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Producto>()
                .HasOne(p => p.Proveedor)
                .WithMany(pr => pr.Productos)
                .HasForeignKey(p => p.ID_PROVEEDOR)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Producto>()
                .HasOne(p => p.UsuarioCreador)
                .WithMany(u => u.ProductosCreados)
                .HasForeignKey(p => p.ID_USUARIO_CREADOR)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Producto>()
                .HasMany(p => p.Imagenes)
                .WithOne(i => i.Producto)
                .HasForeignKey(i => i.ID_PRODUCTO)
                .OnDelete(DeleteBehavior.Cascade);

            // Almacén
            modelBuilder.Entity<Almacen>()
                .HasOne(a => a.Provincia)
                .WithMany(p => p.Almacenes)
                .HasForeignKey(a => a.ID_PROVINCIA)
                .OnDelete(DeleteBehavior.Restrict);

            // Inventario
            modelBuilder.Entity<Inventario>()
                .HasOne(i => i.Producto)
                .WithMany(p => p.Inventarios)
                .HasForeignKey(i => i.ID_PRODUCTO)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Inventario>()
                .HasOne(i => i.Almacen)
                .WithMany(a => a.Inventarios)
                .HasForeignKey(i => i.ID_ALMACEN)
                .OnDelete(DeleteBehavior.Restrict);

            // Movimiento de Inventario
            modelBuilder.Entity<MovimientoInventario>()
                .HasOne(m => m.Producto)
                .WithMany(p => p.MovimientosInventario)
                .HasForeignKey(m => m.ID_PRODUCTO)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MovimientoInventario>()
                .HasOne(m => m.Almacen)
                .WithMany(a => a.MovimientosInventario)
                .HasForeignKey(m => m.ID_ALMACEN)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MovimientoInventario>()
                .HasOne(m => m.Usuario)
                .WithMany(u => u.MovimientosInventario)
                .HasForeignKey(m => m.ID_USUARIO)
                .OnDelete(DeleteBehavior.Restrict);

            // Alerta de Inventario
            modelBuilder.Entity<AlertaInventario>()
                .HasOne(a => a.Inventario)
                .WithMany(i => i.Alertas)
                .HasForeignKey(a => a.ID_INVENTARIO)
                .OnDelete(DeleteBehavior.Cascade);

            // Comunicado
            modelBuilder.Entity<Comunicado>()
                .HasOne(c => c.UsuarioCreador)
                .WithMany(u => u.ComunicadosCreados)
                .HasForeignKey(c => c.ID_USUARIO_CREADOR)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Comunicado>()
                .HasMany(c => c.Segmentos)
                .WithOne(s => s.Comunicado)
                .HasForeignKey(s => s.ID_COMUNICADO)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Comunicado>()
                .HasMany(c => c.Envios)
                .WithOne(e => e.Comunicado)
                .HasForeignKey(e => e.ID_COMUNICADO)
                .OnDelete(DeleteBehavior.Cascade);

            // Segmento de Comunicado
            modelBuilder.Entity<SegmentoComunicado>()
                .HasOne(s => s.Provincia)
                .WithMany(p => p.SegmentosComunicado)
                .HasForeignKey(s => s.ID_PROVINCIA)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            modelBuilder.Entity<SegmentoComunicado>()
                .HasOne(s => s.TipoUsuario)
                .WithMany(t => t.SegmentosComunicado)
                .HasForeignKey(s => s.ID_TIPO_USUARIO)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            // Envío de Comunicado
            modelBuilder.Entity<EnvioComunicado>()
                .HasOne(e => e.UsuarioDestinatario)
                .WithMany(u => u.ComunicadosRecibidos)
                .HasForeignKey(e => e.ID_USUARIO_DESTINATARIO)
                .OnDelete(DeleteBehavior.Restrict);

            // Carrito
            modelBuilder.Entity<Carrito>()
                .HasOne(c => c.Usuario)
                .WithMany(u => u.Carritos)
                .HasForeignKey(c => c.ID_USUARIO)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Carrito>()
                .HasMany(c => c.Items)
                .WithOne(i => i.Carrito)
                .HasForeignKey(i => i.ID_CARRITO)
                .OnDelete(DeleteBehavior.Cascade);

            // Item de Carrito
            modelBuilder.Entity<ItemCarrito>()
                .HasOne(i => i.Producto)
                .WithMany(p => p.ItemsCarrito)
                .HasForeignKey(i => i.ID_PRODUCTO)
                .OnDelete(DeleteBehavior.Restrict);

            // Pedido
            modelBuilder.Entity<Pedido>()
                .HasOne(p => p.Usuario)
                .WithMany(u => u.Pedidos)
                .HasForeignKey(p => p.ID_USUARIO)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Pedido>()
                .HasOne(p => p.Provincia)
                .WithMany(pr => pr.Pedidos)
                .HasForeignKey(p => p.ID_PROVINCIA)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Pedido>()
                .HasMany(p => p.Detalles)
                .WithOne(d => d.Pedido)
                .HasForeignKey(d => d.ID_PEDIDO)
                .OnDelete(DeleteBehavior.Cascade);

            // Detalle de Pedido
            modelBuilder.Entity<DetallePedido>()
                .HasOne(d => d.Producto)
                .WithMany(p => p.DetallesPedido)
                .HasForeignKey(d => d.ID_PRODUCTO)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DetallePedido>()
                .HasOne(d => d.AlmacenOrigen)
                .WithMany(a => a.DetallesPedido)
                .HasForeignKey(d => d.ID_ALMACEN_ORIGEN)
                .OnDelete(DeleteBehavior.Restrict);

            // Reset Password Token
            modelBuilder.Entity<ResetPasswordToken>()
                .HasOne(r => r.Usuario)
                .WithMany()  // No necesitamos una colección en Usuario
                .HasForeignKey(r => r.ID_USUARIO)
                .OnDelete(DeleteBehavior.Cascade);

            // Relaciones para las entidades de WhatsApp
            modelBuilder.Entity<MensajeWhatsApp>()
                .HasOne(m => m.Conversacion)
                .WithMany(c => c.Mensajes)
                .HasForeignKey(m => m.ID_CONVERSACION)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ConversacionWhatsApp>()
                .HasOne(c => c.Usuario)
                .WithMany()  // No necesitamos una colección en Usuario
                .HasForeignKey(c => c.ID_USUARIO)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            // Configuraciones de propiedades (default, tipos, etc.)
            modelBuilder.Entity<Producto>()
                .Property(p => p.ACTIVO)
                .HasDefaultValue(true);

            modelBuilder.Entity<Producto>()
                .Property(p => p.FECHA_CREACION)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<Inventario>()
                .Property(i => i.CANTIDAD_MINIMA)
                .HasDefaultValue(5);

            modelBuilder.Entity<Inventario>()
                .Property(i => i.FECHA_ACTUALIZACION)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<MovimientoInventario>()
                .Property(m => m.FECHA_MOVIMIENTO)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<AlertaInventario>()
                .Property(a => a.FECHA_ALERTA)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<AlertaInventario>()
                .Property(a => a.PROCESADA)
                .HasDefaultValue(false);

            modelBuilder.Entity<Comunicado>()
                .Property(c => c.FECHA_CREACION)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<EnvioComunicado>()
                .Property(e => e.FECHA_ENVIO)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<Carrito>()
                .Property(c => c.FECHA_CREACION)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<Carrito>()
                .Property(c => c.ACTIVO)
                .HasDefaultValue(true);

            modelBuilder.Entity<ItemCarrito>()
                .Property(i => i.CANTIDAD)
                .HasDefaultValue(1);

            modelBuilder.Entity<ItemCarrito>()
                .Property(i => i.FECHA_AGREGADO)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<Pedido>()
                .Property(p => p.FECHA_PEDIDO)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<Pedido>()
                .Property(p => p.ESTADO)
                .HasDefaultValue("PENDIENTE");

            modelBuilder.Entity<ResetPasswordToken>()
                .Property(r => r.UTILIZADO)
                .HasDefaultValue(false);

            // Default para entidades de WhatsApp
            modelBuilder.Entity<ConversacionWhatsApp>()
                .Property(c => c.FECHA_CREACION)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<ConversacionWhatsApp>()
                .Property(c => c.ULTIMA_ACTUALIZACION)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<ConversacionWhatsApp>()
                .Property(c => c.ESTADO)
                .HasDefaultValue("Activo");

            modelBuilder.Entity<ConfiguracionChatbot>()
                .Property(c => c.ACTIVO)
                .HasDefaultValue(true);

            // Configuración de tipos de datos numéricos
            modelBuilder.Entity<Producto>()
                .Property(p => p.PRECIO)
                .HasColumnType("decimal(10,2)");

            modelBuilder.Entity<ItemCarrito>()
                .Property(i => i.PRECIO_UNITARIO)
                .HasColumnType("decimal(10,2)");

            modelBuilder.Entity<Pedido>()
                .Property(p => p.TOTAL)
                .HasColumnType("decimal(10,2)");

            modelBuilder.Entity<DetallePedido>()
                .Property(d => d.PRECIO_UNITARIO)
                .HasColumnType("decimal(10,2)");

            // Configuración de índices
            modelBuilder.Entity<Usuario>()
                .HasIndex(u => u.CORREO_ELECTRONICO)
                .IsUnique();

            modelBuilder.Entity<Usuario>()
                .HasIndex(u => u.NOMBRE_USUARIO)
                .IsUnique();

            modelBuilder.Entity<Producto>()
                .HasIndex(p => p.CODIGO)
                .IsUnique();

            modelBuilder.Entity<Producto>()
                .HasIndex(p => p.ID_CATEGORIA);

            modelBuilder.Entity<Producto>()
                .HasIndex(p => p.ID_MARCA);

            modelBuilder.Entity<Inventario>()
                .HasIndex(i => new { i.ID_PRODUCTO, i.ID_ALMACEN })
                .IsUnique();

            modelBuilder.Entity<MovimientoInventario>()
                .HasIndex(m => m.FECHA_MOVIMIENTO);

            modelBuilder.Entity<Pedido>()
                .HasIndex(p => p.ID_USUARIO);

            modelBuilder.Entity<Pedido>()
                .HasIndex(p => p.ESTADO);

            modelBuilder.Entity<Pedido>()
                .HasIndex(p => p.FECHA_PEDIDO);

            modelBuilder.Entity<ResetPasswordToken>()
                .HasIndex(r => r.TOKEN);

            modelBuilder.Entity<ResetPasswordToken>()
                .HasIndex(r => r.ID_USUARIO);

            // Índices para WhatsApp
            modelBuilder.Entity<ConversacionWhatsApp>()
                .HasIndex(c => c.NUMERO_TELEFONO);

            modelBuilder.Entity<ConversacionWhatsApp>()
                .HasIndex(c => c.ID_USUARIO);

            modelBuilder.Entity<MensajeWhatsApp>()
                .HasIndex(m => m.ID_CONVERSACION);

            modelBuilder.Entity<MensajeWhatsApp>()
                .HasIndex(m => m.FECHA_HORA);

            modelBuilder.Entity<ConfiguracionChatbot>()
                .HasIndex(c => c.TIPO);

            modelBuilder.Entity<ConfiguracionChatbot>()
                .HasIndex(c => c.PALABRA_CLAVE);
        }
    }
}