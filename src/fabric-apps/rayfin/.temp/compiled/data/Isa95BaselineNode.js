var __esDecorate = (this && this.__esDecorate) || function (ctor, descriptorIn, decorators, contextIn, initializers, extraInitializers) {
    function accept(f) { if (f !== void 0 && typeof f !== "function") throw new TypeError("Function expected"); return f; }
    var kind = contextIn.kind, key = kind === "getter" ? "get" : kind === "setter" ? "set" : "value";
    var target = !descriptorIn && ctor ? contextIn["static"] ? ctor : ctor.prototype : null;
    var descriptor = descriptorIn || (target ? Object.getOwnPropertyDescriptor(target, contextIn.name) : {});
    var _, done = false;
    for (var i = decorators.length - 1; i >= 0; i--) {
        var context = {};
        for (var p in contextIn) context[p] = p === "access" ? {} : contextIn[p];
        for (var p in contextIn.access) context.access[p] = contextIn.access[p];
        context.addInitializer = function (f) { if (done) throw new TypeError("Cannot add initializers after decoration has completed"); extraInitializers.push(accept(f || null)); };
        var result = (0, decorators[i])(kind === "accessor" ? { get: descriptor.get, set: descriptor.set } : descriptor[key], context);
        if (kind === "accessor") {
            if (result === void 0) continue;
            if (result === null || typeof result !== "object") throw new TypeError("Object expected");
            if (_ = accept(result.get)) descriptor.get = _;
            if (_ = accept(result.set)) descriptor.set = _;
            if (_ = accept(result.init)) initializers.unshift(_);
        }
        else if (_ = accept(result)) {
            if (kind === "field") initializers.unshift(_);
            else descriptor[key] = _;
        }
    }
    if (target) Object.defineProperty(target, contextIn.name, descriptor);
    done = true;
};
var __runInitializers = (this && this.__runInitializers) || function (thisArg, initializers, value) {
    var useValue = arguments.length > 2;
    for (var i = 0; i < initializers.length; i++) {
        value = useValue ? initializers[i].call(thisArg, value) : initializers[i].call(thisArg);
    }
    return useValue ? value : void 0;
};
import { authenticated, entity, int, text, uuid } from '@microsoft/rayfin-core';
let Isa95BaselineNode = (() => {
    let _classDecorators = [entity(), authenticated('*')];
    let _classDescriptor;
    let _classExtraInitializers = [];
    let _classThis;
    let _id_decorators;
    let _id_initializers = [];
    let _id_extraInitializers = [];
    let _nodeId_decorators;
    let _nodeId_initializers = [];
    let _nodeId_extraInitializers = [];
    let _nodeType_decorators;
    let _nodeType_initializers = [];
    let _nodeType_extraInitializers = [];
    let _parentNodeId_decorators;
    let _parentNodeId_initializers = [];
    let _parentNodeId_extraInitializers = [];
    let _displayName_decorators;
    let _displayName_initializers = [];
    let _displayName_extraInitializers = [];
    let _status_decorators;
    let _status_initializers = [];
    let _status_extraInitializers = [];
    let _version_decorators;
    let _version_initializers = [];
    let _version_extraInitializers = [];
    let _user_id_decorators;
    let _user_id_initializers = [];
    let _user_id_extraInitializers = [];
    var Isa95BaselineNode = class {
        static { _classThis = this; }
        static {
            const _metadata = typeof Symbol === "function" && Symbol.metadata ? Object.create(null) : void 0;
            _id_decorators = [uuid()];
            _nodeId_decorators = [text({ max: 128, unique: true })];
            _nodeType_decorators = [text({ max: 32 })];
            _parentNodeId_decorators = [text({ max: 128, optional: true })];
            _displayName_decorators = [text({ max: 256 })];
            _status_decorators = [text({ max: 16, default: 'Active' })];
            _version_decorators = [int({ default: 1 })];
            _user_id_decorators = [text({ max: 128 })];
            __esDecorate(null, null, _id_decorators, { kind: "field", name: "id", static: false, private: false, access: { has: obj => "id" in obj, get: obj => obj.id, set: (obj, value) => { obj.id = value; } }, metadata: _metadata }, _id_initializers, _id_extraInitializers);
            __esDecorate(null, null, _nodeId_decorators, { kind: "field", name: "nodeId", static: false, private: false, access: { has: obj => "nodeId" in obj, get: obj => obj.nodeId, set: (obj, value) => { obj.nodeId = value; } }, metadata: _metadata }, _nodeId_initializers, _nodeId_extraInitializers);
            __esDecorate(null, null, _nodeType_decorators, { kind: "field", name: "nodeType", static: false, private: false, access: { has: obj => "nodeType" in obj, get: obj => obj.nodeType, set: (obj, value) => { obj.nodeType = value; } }, metadata: _metadata }, _nodeType_initializers, _nodeType_extraInitializers);
            __esDecorate(null, null, _parentNodeId_decorators, { kind: "field", name: "parentNodeId", static: false, private: false, access: { has: obj => "parentNodeId" in obj, get: obj => obj.parentNodeId, set: (obj, value) => { obj.parentNodeId = value; } }, metadata: _metadata }, _parentNodeId_initializers, _parentNodeId_extraInitializers);
            __esDecorate(null, null, _displayName_decorators, { kind: "field", name: "displayName", static: false, private: false, access: { has: obj => "displayName" in obj, get: obj => obj.displayName, set: (obj, value) => { obj.displayName = value; } }, metadata: _metadata }, _displayName_initializers, _displayName_extraInitializers);
            __esDecorate(null, null, _status_decorators, { kind: "field", name: "status", static: false, private: false, access: { has: obj => "status" in obj, get: obj => obj.status, set: (obj, value) => { obj.status = value; } }, metadata: _metadata }, _status_initializers, _status_extraInitializers);
            __esDecorate(null, null, _version_decorators, { kind: "field", name: "version", static: false, private: false, access: { has: obj => "version" in obj, get: obj => obj.version, set: (obj, value) => { obj.version = value; } }, metadata: _metadata }, _version_initializers, _version_extraInitializers);
            __esDecorate(null, null, _user_id_decorators, { kind: "field", name: "user_id", static: false, private: false, access: { has: obj => "user_id" in obj, get: obj => obj.user_id, set: (obj, value) => { obj.user_id = value; } }, metadata: _metadata }, _user_id_initializers, _user_id_extraInitializers);
            __esDecorate(null, _classDescriptor = { value: _classThis }, _classDecorators, { kind: "class", name: _classThis.name, metadata: _metadata }, null, _classExtraInitializers);
            Isa95BaselineNode = _classThis = _classDescriptor.value;
            if (_metadata) Object.defineProperty(_classThis, Symbol.metadata, { enumerable: true, configurable: true, writable: true, value: _metadata });
            __runInitializers(_classThis, _classExtraInitializers);
        }
        id = __runInitializers(this, _id_initializers, void 0);
        nodeId = (__runInitializers(this, _id_extraInitializers), __runInitializers(this, _nodeId_initializers, void 0));
        nodeType = (__runInitializers(this, _nodeId_extraInitializers), __runInitializers(this, _nodeType_initializers, void 0));
        parentNodeId = (__runInitializers(this, _nodeType_extraInitializers), __runInitializers(this, _parentNodeId_initializers, void 0));
        displayName = (__runInitializers(this, _parentNodeId_extraInitializers), __runInitializers(this, _displayName_initializers, void 0));
        status = (__runInitializers(this, _displayName_extraInitializers), __runInitializers(this, _status_initializers, void 0));
        version = (__runInitializers(this, _status_extraInitializers), __runInitializers(this, _version_initializers, void 0));
        user_id = (__runInitializers(this, _version_extraInitializers), __runInitializers(this, _user_id_initializers, void 0));
        constructor() {
            __runInitializers(this, _user_id_extraInitializers);
        }
    };
    return Isa95BaselineNode = _classThis;
})();
export { Isa95BaselineNode };
