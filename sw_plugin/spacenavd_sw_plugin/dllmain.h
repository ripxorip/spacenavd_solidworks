// dllmain.h : Declaration of module class.

class CspacenavdswpluginModule : public ATL::CAtlDllModuleT< CspacenavdswpluginModule >
{
public :
	DECLARE_LIBID(LIBID_spacenavdswpluginLib)
	DECLARE_REGISTRY_APPID_RESOURCEID(IDR_SPACENAVDSWPLUGIN, "{ddcfa78a-bcca-493c-b920-9ca1d5203fa6}")
};

extern class CspacenavdswpluginModule _AtlModule;
