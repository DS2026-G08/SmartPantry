import { ConfigStateService, RoutesService, eLayoutType } from '@abp/ng.core';
import { inject, provideAppInitializer } from '@angular/core';
export const APP_ROUTE_PROVIDER = [
  provideAppInitializer(() => {
    configureRoutes();
  }),
];

const MY_PANTRY_MENU = '::Menu:MyPantry';

function configureRoutes() {
  const routes = inject(RoutesService);
  const configState = inject(ConfigStateService);
  routes.add([
      {
        path: '/',
        name: '::Menu:Home',
        iconClass: 'fas fa-home',
        order: 1,
        layout: eLayoutType.application,
      },
      {
        // Registrar el menú no crea la ruta: la ruta /pantry está en app.routes.ts.
        path: '/pantry',
        name: MY_PANTRY_MENU,
        iconClass: 'fas fa-box-open',
        order: 2,
        layout: eLayoutType.application,
      },
      {
        path: '/books',
        name: '::Menu:Books',
        iconClass: 'fas fa-book',
        layout: eLayoutType.application,
        requiredPolicy: 'SmartPantry.Books',
      },
      {
        path: '/authors',
        name: '::Menu:Authors',
        iconClass: 'fas fa-user-edit',
        layout: eLayoutType.application,
        requiredPolicy: 'SmartPantry.Authors',
      },
  ]);

  // "Mi despensa" no requiere un permiso, sólo sesión: se muestra únicamente a usuarios autenticados.
  configState.getDeep$('currentUser.isAuthenticated').subscribe((isAuthenticated: boolean) => {
    routes.patch(MY_PANTRY_MENU, { invisible: !isAuthenticated });
  });
}
