# Comprobaciones Para Contributors

Usa la comprobación más ligera que corresponda al cambio. Las comprobaciones de compilación Unity y Test Runner son costosas, por lo que se esperan para cambios de comportamiento runtime/editor, no para cada edición de documentación.

## Solo Documentación O Metadatos

```bash
git diff --check
```

Verifica también los enlaces afectados y mantén sincronizados los documentos en inglés/ruso cuando ambos idiomas estén afectados.

## Cambios De Código Runtime O Editor

Ejecuta una comprobación de compilación Unity para el proyecto objetivo e incluye el comando en el pull request. Cuando los tests sean relevantes, ejecuta comprobaciones EditMode y/o PlayMode Test Runner.

Para gates de release solo para maintainers o smoke tests en dispositivos, usa la checklist actual del project maintainer y menciona el comando o entorno en el pull request.

## Antes De Abrir Un Pull Request

- Comprueba `git status` y añade al stage solo los archivos previstos.
- No hagas commit de estado generado por Unity como `Library/`, `Logs/`, `Temp/`, `Obj/` o outputs de builds locales.
- No hagas commit de secretos, archivos `.env`, dumps de dispositivos, logs privados o screenshots no relacionados.
- Si cambia el comportamiento del profiler runtime, actualiza tests y documentación de usuario en el mismo PR.
