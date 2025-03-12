import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class CityStateService {
  private stateCityData: { [key: string]: string[] } = {
    'Aguascalientes': ['Aguascalientes', 'Asientos', 'Calvillo', 'Rincón de Romos'],
    'Baja California': ['Mexicali', 'Tijuana', 'Ensenada', 'Tecate'],
    'Baja California Sur': ['La Paz', 'Cabo San Lucas', 'San José del Cabo', 'Loreto'],
    'Campeche': ['San Francisco de Campeche', 'Campeche', 'Edzná', 'Calkiní'],
    'Chiapas': ['Tuxtla Gutiérrez', 'San Cristóbal de las Casas', 'Tapachula', 'Comitán'],
    'Chihuahua': ['Chihuahua', 'Ciudad Juárez', 'Delicias', 'Parral'],
    'Coahuila': ['Saltillo', 'Torreón', 'Monclova', 'Piedras Negras'],
    'Colima': ['Colima', 'Manzanillo', 'Tecomán', 'Villa de Álvarez'],
    'Durango': ['Durango', 'Gómez Palacio', 'Lerdo', 'El Salto'],
    'Estado de México': ['Toluca', 'Naucalpan', 'Ecatepec', 'Tlalnepantla'],
    'Guanajuato': ['Guanajuato', 'León', 'Irapuato', 'Celaya'],
    'Guerrero': ['Acapulco', 'Chilpancingo', 'Iguala', 'Taxco'],
    'Hidalgo': ['Pachuca', 'Tulancingo', 'Tizayuca', 'Mineral de la Reforma'],
    'Jalisco': ['Guadalajara', 'Zapopan', 'Tlaquepaque', 'Puerto Vallarta'],
    'Mexico City': ['Ciudad de México', 'Azcapotzalco', 'Xochimilco', 'Iztapalapa'],
    'Michoacán': ['Morelia', 'Uruapan', 'Lázaro Cárdenas', 'Zamora'],
    'Morelos': ['Cuernavaca', 'Jiutepec', 'Cuautla', 'Temixco'],
    'Nayarit': ['Tepic', 'Bahía de Banderas', 'Xalisco', 'Tuxpan'],
    'Nuevo León': ['Monterrey', 'San Pedro Garza García', 'Escobedo', 'Apodaca'],
    'Oaxaca': ['Oaxaca de Juárez', 'San Bartolo Coyotepec', 'Juchitán de Zaragoza', 'Tlacolula de Matamoros'],
    'Puebla': ['Puebla', 'Cholula', 'Tehuacán', 'Atlixco'],
    'Querétaro': ['Querétaro', 'El Marqués', 'San Juan del Río', 'El Pueblito'],
    'Quintana Roo': ['Cancún', 'Playa del Carmen', 'Chetumal', 'Tulum'],
    'San Luis Potosí': ['San Luis Potosí', 'Ciudad Valles', 'Matehuala', 'Rioverde'],
    'Sinaloa': ['Culiacán', 'Mazatlán', 'Los Mochis', 'Guasave'],
    'Sonora': ['Hermosillo', 'Nogales', 'Ciudad Obregón', 'San Luis Río Colorado'],
    'Tabasco': ['Villahermosa', 'Cárdenas', 'Comalcalco', 'Paraíso'],
    'Tamaulipas': ['Ciudad Victoria', 'Reynosa', 'Matamoros', 'Nuevo Laredo'],
    'Tlaxcala': ['Tlaxcala', 'Apizaco', 'San Pablo del Monte', 'Calpulalpan'],
    'Veracruz': ['Veracruz', 'Xalapa', 'Coatzacoalcos', 'Poza Rica'],
    'Yucatán': ['Mérida', 'Valladolid', 'Progreso', 'Tizimin'],
    'Zacatecas': ['Zacatecas', 'Guadalupe', 'Fresnillo', 'Jerez']
  };

  constructor() { }

  getStates(): Observable<string[]> {
    return of(Object.keys(this.stateCityData));
  }

  getCitiesByState(state: string): Observable<string[]> {
    return of(this.stateCityData[state] || []);
  }
}
