<template>
  <div>
    <a-card>
      <AgSearchForm
        :searchData="searchData"
        :openIsShowMore="false"
        :isShowMore="isShowMore"
        :btnLoading="btnLoading"
        @update-search-data="handleSearchFormData"
        @set-is-show-more="setIsShowMore"
        @query-func="queryFunc">
        <template slot="formItem">
          <ag-text-up :placeholder="'支付方式代码'" :msg="searchData.wayCode" v-model="searchData.wayCode" />
          <ag-text-up :placeholder="'支付方式名称'" :msg="searchData.wayName" v-model="searchData.wayName" />
          <a-form-item label="" class="table-head-layout">
            <a-select v-model="searchData.wayType" placeholder="支付类型" default-value="">
              <a-select-option value="">全部</a-select-option>
              <a-select-option value="WECHAT">微信</a-select-option>
              <a-select-option value="ALIPAY">支付宝</a-select-option>
              <a-select-option value="YSFPAY">云闪付</a-select-option>
              <a-select-option value="UNIONPAY">银联</a-select-option>
              <a-select-option value="DCEPPAY">数字人民币</a-select-option>
              <a-select-option value="OTHER">其他</a-select-option>
            </a-select>
          </a-form-item>
        </template>
      </AgSearchForm>
      <!-- 列表渲染 -->
      <AgTable
        @btnLoadClose="btnLoading=false"
        ref="infoTable"
        :initData="true"
        :reqTableDataFunc="reqTableDataFunc"
        :tableColumns="tableColumns"
        :searchData="searchData"
        rowKey="wayCode"
      >
        <template slot="topLeftSlot">
          <div>
            <a-button v-if="$access('ENT_PC_WAY_ADD')" type="primary" icon="plus" @click="addFunc" class="mg-b-30">新建</a-button>
          </div>
        </template>
        <template slot="wayCodeSlot" slot-scope="{record}"><b>{{ record.wayCode }}</b></template> <!-- 自定义插槽 -->
        <template slot="wayTypeSlot" slot-scope="{record}">
          <a-tag
            :key="record.wayType"
            :color="record.wayType === 'WECHAT' ? 'rgb(4, 190, 2)' :
              record.wayType === 'ALIPAY' ? 'rgb(23, 121, 255)' :
              record.wayType === 'YSFPAY' ? '#f5222d' :
              record.wayType === 'UNIONPAY' ? '#00508e' :
              record.wayType === 'DCEPPAY' ? '#d12c2c' : '#fa8c16'">
            {{ record.wayType === 'WECHAT' ? '微信' :
              record.wayType === 'ALIPAY' ? '支付宝' :
              record.wayType === 'YSFPAY' ? '云闪付' :
              record.wayType === 'UNIONPAY' ? '银联' :
              record.wayType === 'DCEPPAY' ? '数字人民币' : '其他' }}
          </a-tag>
        </template>
        <template slot="opSlot" slot-scope="{record}">  <!-- 操作列插槽 -->
          <AgTableColumns>
            <a-button type="link" v-if="$access('ENT_PC_WAY_EDIT')" @click="editFunc(record.wayCode)">修改</a-button>
            <a-button type="link" style="color: red" v-if="$access('ENT_PC_WAY_DEL')" @click="delFunc(record.wayCode)">删除</a-button>
          </AgTableColumns>
        </template>
      </AgTable>
    </a-card>
    <!-- 新增页面组件  -->
    <InfoAddOrEdit ref="infoAddOrEdit" :callbackFunc="queryFunc"/>
  </div>

</template>
<script>
import AgSearchForm from '@/components/AgSearch/AgSearchForm'
import AgTable from '@/components/AgTable/AgTable'
import AgTableColumns from '@/components/AgTable/AgTableColumns'
import { API_URL_PAYWAYS_LIST, req } from '@/api/manage'
import InfoAddOrEdit from './AddOrEdit'
import AgTextUp from '@/components/AgTextUp/AgTextUp' // 文字上移组件
// eslint-disable-next-line no-unused-vars
const tableColumns = [
  { key: 'wayCode', fixed: 'left', title: '支付方式代码', scopedSlots: { customRender: 'wayCodeSlot' } },
  { key: 'wayName', dataIndex: 'wayName', title: '支付方式名称' },
  { key: 'wayType', title: '支付类型', align: 'center', scopedSlots: { customRender: 'wayTypeSlot' } },
  { key: 'op', title: '操作', width: 160, fixed: 'right', align: 'center', scopedSlots: { customRender: 'opSlot' } }
]

export default {
  name: 'PayWayPage',
  components: { AgSearchForm, AgTable, AgTableColumns, InfoAddOrEdit, AgTextUp },
  data () {
    return {
      isShowMore: false,
      btnLoading: false,
      tableColumns: tableColumns,
      searchData: {}
    }
  },
  methods: {
    handleSearchFormData (searchData) {
      this.searchData = searchData
    },
    setIsShowMore (isShowMore) {
      this.isShowMore = isShowMore
    },
    // 请求table接口数据
    reqTableDataFunc: (params) => {
      return req.list(API_URL_PAYWAYS_LIST, params)
    },
    queryFunc () { // 点击【查询】按钮点击事件
      this.btnLoading = true
      this.$refs.infoTable.refTable(true)
    },
    addFunc: function () { // 业务通用【新增】 函数
      this.$refs.infoAddOrEdit.show()
    },
    editFunc: function (wayCode) { // 业务通用【修改】 函数
      this.$refs.infoAddOrEdit.show(wayCode)
    },
    delFunc: function (wayCode) {
      const that = this
      this.$infoBox.confirmDanger('确认删除？', '', () => {
        req.delById(API_URL_PAYWAYS_LIST, wayCode).then(res => {
          that.$message.success('删除成功！')
          that.$refs.infoTable.refTable(false)
        })
      })
    }
  }
}
</script>
